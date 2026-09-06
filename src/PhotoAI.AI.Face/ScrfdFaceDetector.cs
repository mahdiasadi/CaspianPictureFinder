using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using PhotoAI.Core.Inference;
using PhotoAI.Core.Interfaces;

namespace PhotoAI.AI.Face;

public class ScrfdFaceDetector : IFaceDetector
{
    private readonly IInferenceEngine _engine;
    private IInferenceSession? _session;
    private bool _disposed;

    public string ModelId => "scrfd-10g";
    public string ModelName => "SCRFD 10G";
    public string Version => "1.0";
    public bool IsLoaded => _session != null;

    private const int InputSize = 640;
    private const float ScoreThreshold = 0.5f;
    private const float NmsThreshold = 0.4f;

    public ScrfdFaceDetector(IInferenceEngine engine)
    {
        _engine = engine;
    }

    public async Task LoadAsync(string modelPath, ProcessingBackend backend = ProcessingBackend.Auto, CancellationToken cancellationToken = default)
    {
        if (_session != null) return;

        var selectedEngine = backend switch
        {
            ProcessingBackend.Cuda => InferenceEngineFactory.Create(ProcessingBackend.Cuda),
            ProcessingBackend.Cpu => InferenceEngineFactory.Create(ProcessingBackend.Cpu),
            _ => _engine
        };

        _session = await selectedEngine.CreateSessionAsync(modelPath, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<FaceDetectionResult>> DetectAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        EnsureLoaded();
        using var ms = new MemoryStream(imageData);
        return await DetectAsync(ms, cancellationToken);
    }

    public async Task<IReadOnlyList<FaceDetectionResult>> DetectAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        EnsureLoaded();
        
        // Preprocess image
        var (preprocessedTensor, scale, padX, padY) = await PreprocessImageAsync(imageStream, cancellationToken);
        
        // Run inference
        var inputName = _session!.InputNames[0];
        var inputTensor = NamedOnnxValue.CreateFromTensor(inputName, new DenseTensor<float>(preprocessedTensor, new[] { 1, 3, InputSize, InputSize }));
        
        var inputs = new[] { inputTensor };
        var result = await _session.RunAsync(inputs, cancellationToken);
        
        // Post-process detections
        var detections = PostProcess(result, scale, padX, padY);
        return detections;
    }

    public void Unload()
    {
        _session?.Dispose();
        _session = null;
    }

    private void EnsureLoaded()
    {
        if (_session == null)
            throw new InvalidOperationException("Model not loaded. Call LoadAsync first.");
    }

    private async Task<(float[] tensor, float scale, int padX, int padY)> PreprocessImageAsync(Stream imageStream, CancellationToken cancellationToken)
    {
        using var image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(imageStream, cancellationToken);
        
        // Resize with letterboxing to maintain aspect ratio
        float scale = Math.Min((float)InputSize / image.Width, (float)InputSize / image.Height);
        int newWidth = (int)(image.Width * scale);
        int newHeight = (int)(image.Height * scale);
        int padX = (InputSize - newWidth) / 2;
        int padY = (InputSize - newHeight) / 2;

        var resized = image.Clone(ctx => ctx
            .Resize(newWidth, newHeight)
            .BackgroundColor(Rgba32.ParseHex("114114"))); // Dark gray padding

        var padded = new Image<Rgba32>(InputSize, InputSize);
        padded.Mutate(ctx => ctx.DrawImage(resized, new Point(padX, padY), 1f));

        // Convert to CHW format, normalize to [0, 1]
        var tensor = new float[3 * InputSize * InputSize];
        int idx = 0;
        
        padded.ProcessPixelRows(accessor =>
        {
            for (int c = 0; c < 3; c++)
            {
                for (int y = 0; y < InputSize; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < InputSize; x++)
                    {
                        var pixel = row[x];
                        float val = c switch
                        {
                            0 => pixel.R / 255f,
                            1 => pixel.G / 255f,
                            _ => pixel.B / 255f
                        };
                        tensor[idx++] = val;
                    }
                }
            }
        });

        return (tensor, scale, padX, padY);
    }

    private IReadOnlyList<FaceDetectionResult> PostProcess(InferenceResult result, float scale, int padX, int padY)
    {
        // SCRFD outputs: [batch, num_anchors, 5] (x, y, w, h, score) and [batch, num_anchors, 10] (landmarks)
        // This is a simplified version - actual SCRFD has multiple stride outputs
        
        var outputs = result.Outputs;
        if (outputs.Count == 0) return Array.Empty<FaceDetectionResult>();

        var detections = new List<FaceDetectionResult>();

        // Simplified: assume first output is [1, num_detections, 5] (x, y, w, h, score)
        var outputTensor = outputs[0].AsTensor<float>();
        var shape = outputTensor.Dimensions.ToArray();
        
        if (shape.Length >= 3)
        {
            int numDetections = shape[1];
            
            for (int i = 0; i < numDetections; i++)
            {
                float score = outputTensor[0, i, 4];
                if (score < ScoreThreshold) continue;

                float cx = (outputTensor[0, i, 0] * InputSize - padX) / scale;
                float cy = (outputTensor[0, i, 1] * InputSize - padY) / scale;
                float w = outputTensor[0, i, 2] * InputSize / scale;
                float h = outputTensor[0, i, 3] * InputSize / scale;

                detections.Add(new FaceDetectionResult
                {
                    BoundingBoxX = (int)(cx - w / 2),
                    BoundingBoxY = (int)(cy - h / 2),
                    BoundingBoxWidth = (int)w,
                    BoundingBoxHeight = (int)h,
                    Confidence = score
                });
            }
        }

        // Apply NMS
        return ApplyNms(detections);
    }

    private IReadOnlyList<FaceDetectionResult> ApplyNms(List<FaceDetectionResult> detections)
    {
        var sorted = detections.OrderByDescending(d => d.Confidence).ToList();
        var keep = new List<FaceDetectionResult>();

        foreach (var det in sorted)
        {
            bool suppressed = false;
            foreach (var kept in keep)
            {
                if (IoU(det, kept) > NmsThreshold)
                {
                    suppressed = true;
                    break;
                }
            }
            if (!suppressed) keep.Add(det);
        }

        return keep;
    }

    private static float IoU(FaceDetectionResult a, FaceDetectionResult b)
    {
        int x1 = Math.Max(a.BoundingBoxX, b.BoundingBoxX);
        int y1 = Math.Max(a.BoundingBoxY, b.BoundingBoxY);
        int x2 = Math.Min(a.BoundingBoxX + a.BoundingBoxWidth, b.BoundingBoxX + b.BoundingBoxWidth);
        int y2 = Math.Min(a.BoundingBoxY + a.BoundingBoxHeight, b.BoundingBoxY + b.BoundingBoxHeight);

        int inter = Math.Max(0, x2 - x1) * Math.Max(0, y2 - y1);
        int areaA = a.BoundingBoxWidth * a.BoundingBoxHeight;
        int areaB = b.BoundingBoxWidth * b.BoundingBoxHeight;

        return (float)inter / (areaA + areaB - inter);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _session?.Dispose();
            _disposed = true;
        }
    }
}