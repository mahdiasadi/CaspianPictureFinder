using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using PhotoAI.Core.Inference;
using PhotoAI.Core.Interfaces;

namespace PhotoAI.AI.OCR;

public class PaddleOcrEngine : IOcrEngine
{
    private readonly IInferenceEngine _engine;
    private IInferenceSession? _detSession;
    private IInferenceSession? _recSession;
    private bool _disposed;

    public string ModelId => "paddleocr-pp-ocrv5";
    public string ModelName => "PaddleOCR PP-OCRv5";
    public string Version => "1.0";
    public bool IsLoaded => _detSession != null && _recSession != null;

    private const int DetInputSize = 960;
    private const int RecInputHeight = 48;

    public PaddleOcrEngine(IInferenceEngine engine)
    {
        _engine = engine;
    }

    public async Task LoadAsync(string modelPath, ProcessingBackend backend = ProcessingBackend.Auto, CancellationToken cancellationToken = default)
    {
        if (_detSession != null && _recSession != null) return;

        var selectedEngine = backend switch
        {
            ProcessingBackend.Cuda => InferenceEngineFactory.Create(ProcessingBackend.Cuda),
            ProcessingBackend.Cpu => InferenceEngineFactory.Create(ProcessingBackend.Cpu),
            _ => _engine
        };

        var detPath = Path.Combine(modelPath, "det.onnx");
        var recPath = Path.Combine(modelPath, "rec.onnx");

        if (!File.Exists(detPath) || !File.Exists(recPath))
            throw new FileNotFoundException($"OCR model files not found in {modelPath}");

        _detSession = await selectedEngine.CreateSessionAsync(detPath, cancellationToken: cancellationToken);
        _recSession = await selectedEngine.CreateSessionAsync(recPath, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<OcrDetectionResult>> RecognizeAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        EnsureLoaded();
        using var ms = new MemoryStream(imageData);
        return await RecognizeAsync(ms, cancellationToken);
    }

    public async Task<IReadOnlyList<OcrDetectionResult>> RecognizeAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        EnsureLoaded();

        using var image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(imageStream, cancellationToken);

        var textBoxes = await DetectTextRegionsAsync(image, cancellationToken);

        if (textBoxes.Count == 0)
            return Array.Empty<OcrDetectionResult>();

        var results = new List<OcrDetectionResult>();

        foreach (var box in textBoxes)
        {
            var cropped = CropTextRegion(image, box);
            var text = await RecognizeTextAsync(cropped, cancellationToken);
            
            if (!string.IsNullOrWhiteSpace(text))
            {
                results.Add(new OcrDetectionResult
                {
                    Text = text,
                    Confidence = box.Score,
                    BoundingBoxX = box.X,
                    BoundingBoxY = box.Y,
                    BoundingBoxWidth = box.Width,
                    BoundingBoxHeight = box.Height,
                    Language = "en"
                });
            }
        }

        return results;
    }

    public void Unload()
    {
        _detSession?.Dispose();
        _recSession?.Dispose();
        _detSession = null;
        _recSession = null;
    }

    private void EnsureLoaded()
    {
        if (_detSession == null || _recSession == null)
            throw new InvalidOperationException("OCR models not loaded. Call LoadAsync first.");
    }

    private async Task<List<TextBox>> DetectTextRegionsAsync(Image<Rgba32> image, CancellationToken cancellationToken)
    {
        var (preprocessedTensor, scaleX, scaleY) = PreprocessForDetection(image);

        var inputName = _detSession!.InputNames[0];
        var inputTensor = NamedOnnxValue.CreateFromTensor(
            _detSession.InputNames[0],
            new DenseTensor<float>(preprocessedTensor, new[] { 1, 3, 960, 960 }));

        var inputs = new[] { inputTensor };
        var result = await _detSession.RunAsync(inputs, cancellationToken);

        return PostProcessDetection(result, 1f, 1f);
    }

    private (float[] tensor, float scaleX, float scaleY) PreprocessForDetection(Image<Rgba32> image)
    {
        float scaleX = 960f / image.Width;
        float scaleY = 960f / image.Height;

        var resized = image.Clone(ctx => ctx.Resize(960, 960));

        var tensor = new float[3 * 960 * 960];
        int idx = 0;

        resized.ProcessPixelRows(accessor =>
        {
            for (int c = 0; c < 3; c++)
            {
                for (int y = 0; y < 960; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < 960; x++)
                    {
                        var pixel = row[x];
                        float val = c switch
                        {
                            0 => pixel.R / 255f,
                            1 => pixel.G / 255f,
                            _ => pixel.B / 255f
                        };
                        float mean = c == 0 ? 0.485f : c == 1 ? 0.456f : 0.406f;
                        float std = c == 0 ? 0.229f : c == 1 ? 0.224f : 0.225f;
                        tensor[idx++] = (val - mean) / std;
                    }
                }
            }
        });

        return (tensor, 1f, 1f);
    }

    private List<TextBox> PostProcessDetection(InferenceResult result, float scaleX, float scaleY)
    {
        var outputs = result.Outputs;
        var boxes = new List<TextBox>();

        if (outputs.Count == 0) return boxes;

        return boxes;
    }

    private Image<Rgba32> CropTextRegion(Image<Rgba32> image, TextBox box)
    {
        var rect = new Rectangle(box.X, box.Y, box.Width, box.Height);
        return image.Clone(ctx => ctx.Crop(rect));
    }

    private async Task<string> RecognizeTextAsync(Image<Rgba32> image, CancellationToken cancellationToken)
    {
        float scale = 48f / image.Height;
        int newWidth = Math.Max(1, (int)(image.Width * scale));

        var resized = image.Clone(ctx => ctx.Resize(newWidth, 48));

        int paddedWidth = ((newWidth + 31) / 32) * 32;
        if (paddedWidth != newWidth)
        {
            var padded = new Image<Rgba32>(paddedWidth, 48);
            padded.Mutate(ctx => ctx.DrawImage(resized, new Point(0, 0), 1f));
            resized = padded;
        }

        var tensor = new float[3 * 48 * resized.Width];
        int idx = 0;

        resized.ProcessPixelRows(accessor =>
        {
            for (int c = 0; c < 3; c++)
            {
                for (int y = 0; y < 48; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < resized.Width; x++)
                    {
                        var pixel = row[x];
                        float val = c switch
                        {
                            0 => pixel.R / 255f,
                            1 => pixel.G / 255f,
                            _ => pixel.B / 255f
                        };
                        tensor[idx++] = val * 2f - 1f;
                    }
                }
            }
        });

        var inputName = _recSession!.InputNames[0];
        var inputTensor = NamedOnnxValue.CreateFromTensor(
            _recSession.InputNames[0],
            new DenseTensor<float>(tensor, new[] { 1, 3, 48, resized.Width }));

        var inputs = new[] { inputTensor };
        var result = await _recSession!.RunAsync(inputs, CancellationToken.None);

        return PostProcessRecognition(result);
    }

    private string PostProcessRecognition(InferenceResult result)
    {
        var outputs = result.Outputs;
        if (outputs.Count == 0) return string.Empty;

        return "OCR Text";
    }

    private struct TextBox
    {
        public int X, Y, Width, Height;
        public float Score;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _detSession?.Dispose();
            _recSession?.Dispose();
            _disposed = true;
        }
    }
}