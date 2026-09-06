using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using PhotoAI.Core.Inference;
using PhotoAI.Core.Interfaces;

namespace PhotoAI.AI.Vision;

public class DocumentDetector : IObjectDetector
{
    private readonly IInferenceEngine _engine;
    private IInferenceSession? _session;
    private bool _disposed;

    public string ModelId => "document-detector";
    public string ModelName => "Document/Screenshot Detector";
    public string Version => "1.0";
    public bool IsLoaded => _session != null;

    private const int InputSize = 640;
    private const float ScoreThreshold = 0.5f;

    private static readonly string[] _labels = new[]
    {
        "document", "screenshot", "receipt", "id_card", "passport", "business_card",
        "license_plate", "credit_card", "qr_code", "barcode", "form", "table"
    };

    public DocumentDetector(IInferenceEngine engine)
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

    public async Task<IReadOnlyList<ObjectDetectionResult>> DetectAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        EnsureLoaded();
        using var ms = new MemoryStream(imageData);
        return await DetectAsync(ms, cancellationToken);
    }

    public async Task<IReadOnlyList<ObjectDetectionResult>> DetectAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        EnsureLoaded();

        var (preprocessedTensor, scaleX, scaleY) = await PreprocessImageAsync(imageStream, cancellationToken);

        var inputName = _session!.InputNames[0];
        var inputTensor = NamedOnnxValue.CreateFromTensor(
            _session.InputNames[0],
            new DenseTensor<float>(preprocessedTensor, new[] { 1, 3, InputSize, InputSize }));

        var inputs = new[] { inputTensor };
        var result = await _session.RunAsync(inputs, cancellationToken);

        return PostProcess(result, scaleX, scaleY);
    }

    public Task<IReadOnlyList<string>> GetSupportedLabelsAsync()
    {
        return Task.FromResult((IReadOnlyList<string>)_labels);
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

    private async Task<(float[] tensor, float scaleX, float scaleY)> PreprocessImageAsync(Stream imageStream, CancellationToken cancellationToken)
    {
        using var image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(imageStream, cancellationToken);

        float scaleX = (float)InputSize / image.Width;
        float scaleY = (float)InputSize / image.Height;

        var resized = image.Clone(ctx => ctx.Resize(InputSize, InputSize));

        var tensor = new float[3 * InputSize * InputSize];
        int idx = 0;

        resized.ProcessPixelRows(accessor =>
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

        return (tensor, scaleX, scaleY);
    }

    private IReadOnlyList<ObjectDetectionResult> PostProcess(InferenceResult result, float scaleX, float scaleY)
    {
        var detections = new List<ObjectDetectionResult>();

        var outputs = result.Outputs;
        if (outputs.Count < 2) return detections;

        var boxesTensor = outputs[0].AsTensor<float>();
        var logitsTensor = outputs[1].AsTensor<float>();

        var boxesShape = boxesTensor.Dimensions.ToArray();
        var logitsShape = logitsTensor.Dimensions.ToArray();

        if (boxesShape.Length < 3 || logitsShape.Length < 3) return detections;

        int numQueries = boxesShape[1];
        int numClasses = Math.Min(logitsShape[2], _labels.Length);

        for (int i = 0; i < numQueries; i++)
        {
            float maxScore = 0;
            int maxClass = -1;

            for (int c = 0; c < numClasses; c++)
            {
                float score = 1f / (1f + MathF.Exp(-logitsTensor[0, i, c]));
                if (score > maxScore)
                {
                    maxScore = score;
                    maxClass = c;
                }
            }

            if (maxScore < 0.5f || maxClass < 0 || maxClass >= _labels.Length)
                continue;

            float cx = boxesTensor[0, i, 0];
            float cy = boxesTensor[0, i, 1];
            float w = boxesTensor[0, i, 2];
            float h = boxesTensor[0, i, 3];

            float x = (cx - w / 2f) * InputSize;
            float y = (cy - h / 2f) * InputSize;
            float bw = w * InputSize;
            float bh = h * InputSize;

            x = x / scaleX;
            y = y / scaleY;
            bw = bw / scaleX;
            bh = bh / scaleY;

            detections.Add(new ObjectDetectionResult
            {
                Label = _labels[maxClass],
                Confidence = maxScore,
                BoundingBoxX = (int)x,
                BoundingBoxY = (int)y,
                BoundingBoxWidth = (int)bw,
                BoundingBoxHeight = (int)bh
            });
        }

        return ApplyNms(detections);
    }

    private IReadOnlyList<ObjectDetectionResult> ApplyNms(List<ObjectDetectionResult> detections)
    {
        var result = new List<ObjectDetectionResult>();
        var byClass = detections.GroupBy(d => d.Label);

        foreach (var group in byClass)
        {
            var sorted = group.OrderByDescending(d => d.Confidence).ToList();
            var keep = new List<ObjectDetectionResult>();

            foreach (var det in sorted)
            {
                bool suppressed = false;
                foreach (var kept in keep)
                {
                    if (IoU(det, kept) > 0.45f)
                    {
                        suppressed = true;
                        break;
                    }
                }
                if (!suppressed) keep.Add(det);
            }

            result.AddRange(keep);
        }

        return result;
    }

    private static float IoU(ObjectDetectionResult a, ObjectDetectionResult b)
    {
        int x1 = Math.Max(a.BoundingBoxX, b.BoundingBoxX);
        int y1 = Math.Max(a.BoundingBoxY, b.BoundingBoxY);
        int x2 = Math.Min(a.BoundingBoxX + a.BoundingBoxWidth, b.BoundingBoxX + b.BoundingBoxWidth);
        int y2 = Math.Min(a.BoundingBoxY + a.BoundingBoxHeight, b.BoundingBoxY + b.BoundingBoxHeight);

        int inter = Math.Max(0, x2 - x1) * Math.Max(0, y2 - y1);
        int areaA = a.BoundingBoxWidth * a.BoundingBoxHeight;
        int areaB = b.BoundingBoxWidth * b.BoundingBoxHeight;

        return inter > 0 ? (float)inter / (areaA + areaB - inter) : 0f;
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