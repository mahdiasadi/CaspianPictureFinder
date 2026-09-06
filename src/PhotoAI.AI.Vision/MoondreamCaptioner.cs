using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using PhotoAI.Core.Inference;
using PhotoAI.Core.Interfaces;

namespace PhotoAI.AI.Vision;

public class MoondreamCaptioner : IImageCaptioner
{
    private readonly IInferenceEngine _engine;
    private IInferenceSession? _session;
    private bool _disposed;

    public string ModelId => "moondream";
    public string ModelName => "Moondream";
    public string Version => "1.0";
    public bool IsLoaded => _session != null;

    private const int InputSize = 378;

    public MoondreamCaptioner(IInferenceEngine engine)
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

    public async Task<string> GenerateCaptionAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        EnsureLoaded();
        using var ms = new MemoryStream(imageData);
        return await GenerateCaptionAsync(ms, cancellationToken);
    }

    public async Task<string> GenerateCaptionAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        EnsureLoaded();

        // Preprocess image
        var preprocessedTensor = await PreprocessImageAsync(imageStream, cancellationToken);

        var inputName = _session!.InputNames[0];
        var inputTensor = NamedOnnxValue.CreateFromTensor(
            _session.InputNames[0],
            new DenseTensor<float>(preprocessedTensor, new[] { 1, 3, InputSize, InputSize }));

        var inputs = new[] { inputTensor };
        var result = await _session.RunAsync(inputs, cancellationToken);

        return PostProcessCaption(result);
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

    private async Task<float[]> PreprocessImageAsync(Stream imageStream, CancellationToken cancellationToken)
    {
        using var image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(imageStream, cancellationToken);

        // Resize to 378x378 with center crop
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(InputSize, InputSize),
            Mode = ResizeMode.Crop,
            Position = AnchorPositionMode.Center
        }));

        // CLIP normalization
        var mean = new float[] { 0.48145466f, 0.4578275f, 0.40821073f };
        var std = new float[] { 0.26862954f, 0.26130258f, 0.27577711f };

        var tensor = new float[3 * InputSize * InputSize];
        int idx = 0;

        image.ProcessPixelRows(accessor =>
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
                        tensor[idx++] = (val - mean[c]) / std[c];
                    }
                }
            }
        });

        return tensor;
    }

    private string PostProcessCaption(InferenceResult result)
    {
        // Moondream outputs token IDs - real implementation would decode using tokenizer
        // This is a placeholder - real implementation would use tokenizer decode
        return "A photo containing various objects and scenery.";
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