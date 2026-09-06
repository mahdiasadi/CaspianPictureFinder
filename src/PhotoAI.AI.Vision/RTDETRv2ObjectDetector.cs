using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using PhotoAI.Core.Inference;
using PhotoAI.Core.Interfaces;

namespace PhotoAI.AI.Vision;

public class RTDETRv2ObjectDetector : IObjectDetector
{
    private readonly IInferenceEngine _engine;
    private IInferenceSession? _session;
    private bool _disposed;

    public string ModelId => "rtdetrv2-x";
    public string ModelName => "RTDETRv2-X";
    public string Version => "1.0";
    public bool IsLoaded => _session != null;

    private const int InputSize = 640;
    private const float ScoreThreshold = 0.5f;

    // COCO 80 class labels
    private static readonly string[] _labels = new[]
    {
        "person", "bicycle", "car", "motorcycle", "airplane", "bus", "train", "truck", "boat",
        "traffic light", "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat",
        "dog", "horse", "sheep", "cow", "elephant", "bear", "zebra", "giraffe", "backpack",
        "umbrella", "handbag", "tie", "suitcase", "frisbee", "skis", "snowboard", "sports ball",
        "kite", "baseball bat", "baseball glove", "skateboard", "surfboard", "tennis racket",
        "bottle", "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple",
        "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", "cake",
        "chair", "couch", "potted plant", "bed", "dining table", "toilet", "tv", "laptop",
        "mouse", "remote", "keyboard", "cell phone", "microwave", "oven", "toaster", "sink",
        "refrigerator", "book", "clock", "vase", "scissors", "teddy bear", "hair drier", "toothbrush"
    };

    public RTDETRv2ObjectDetector(IInferenceEngine engine)
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

        var (preprocessedTensor, scale, padX, padY) = await PreprocessImageAsync(imageStream, cancellationToken);

        var inputName = _session!.InputNames[0];
        var inputTensor = NamedOnnxValue.CreateFromTensor(
            _session.InputNames[0],
            new DenseTensor<float>(preprocessedTensor, new[] { 1, 3, InputSize, InputSize }));

        var inputs = new[] { inputTensor };
        var result = await _session.RunAsync(inputs, cancellationToken);

        return PostProcess(result, scale, padX, padY);
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

    private async Task<(float[] tensor, float scale, int padX, int padY)> PreprocessImageAsync(Stream imageStream, CancellationToken cancellationToken)
    {
        using var image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(imageStream, cancellationToken);

        float scale = Math.Min((float)InputSize / image.Width, (float)InputSize / image.Height);
        int newWidth = (int)(image.Width * scale);
        int newHeight = (int)(image.Height * scale);
        int padX = (InputSize - newWidth) / 2;
        int padY = (InputSize - newHeight) / 2;

        var resized = image.Clone(ctx => ctx
            .Resize(newWidth, newHeight)
            .BackgroundColor(Rgba32.ParseHex("114114")));

        var padded = new Image<Rgba32>(InputSize, InputSize);
        padded.Mutate(ctx => ctx.DrawImage(resized, new Point(padX, padY), 1f));

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

    private IReadOnlyList<ObjectDetectionResult> PostProcess(InferenceResult result, float scale, int padX, int padY)
    {
        var detections = new List<ObjectDetectionResult>();

        // RTDETR outputs: [1, num_queries, 4] boxes (cx, cy, w, h) normalized 0-1
        // [1, num_queries, num_classes] logits
        
        var outputs = result.Outputs;
        if (outputs.Count < 2) return detections;

        var boxesTensor = outputs[0].AsTensor<float>(); // [1, 300, 4]
        var logitsTensor = outputs[1].AsTensor<float>(); // [1, 300, 80]

        var boxesShape = boxesTensor.Dimensions.ToArray();
        var logitsShape = logitsTensor.Dimensions.ToArray();

        if (boxesShape.Length < 3 || logitsShape.Length < 3) return detections;

        int numQueries = boxesShape[1];
        int numClasses = logitsShape[2];

        for (int i = 0; i < numQueries; i++)
        {
            // Find max class score
            float maxScore = 0;
            int maxClass = -1;

            for (int c = 0; c < numClasses; c++)
            {
                // Apply sigmoid to logits
                float score = 1f / (1f + MathF.Exp(-logitsTensor[0, i, c]));
                if (score > maxScore)
                {
                    maxScore = score;
                    maxClass = c;
                }
            }

            if (maxScore < ScoreThreshold || maxClass < 0 || maxClass >= _labels.Length)
                continue;

            // Box: [cx, cy, w, h] normalized
            float cx = boxesTensor[0, i, 0];
            float cy = boxesTensor[0, i, 1];
            float w = boxesTensor[0, i, 2];
            float h = boxesTensor[0, i, 3];

            // Convert to pixel coordinates
            float x = (cx - w / 2f) * InputSize;
            float y = (cy - h / 2f) * InputSize;
            float bw = w * InputSize;
            float bh = h * InputSize;

            // Remove padding and scale back
            x = (x - padX) / scale;
            y = (y - padY) / scale;
            bw = bw / scale;
            bh = bh / scale;

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

        // Apply NMS per class
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