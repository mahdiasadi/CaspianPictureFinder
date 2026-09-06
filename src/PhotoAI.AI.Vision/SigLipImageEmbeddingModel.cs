using Microsoft.ML.OnnxRuntime;
using PhotoAI.Core.Inference;
using PhotoAI.Core.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PhotoAI.AI.Vision;

public class SigLipImageEmbeddingModel : IImageEmbeddingModel
{
    private readonly IInferenceEngine _engine;
    private IInferenceSession? _session;
    private bool _disposed;

    public string ModelId => "siglip2-so400m-patch16-256";
    public string ModelName => "SigLIP2 SO400M Patch16";
    public string Version => "1.0";
    public int EmbeddingDimension => 768;
    public bool IsLoaded => _session != null;

    public SigLipImageEmbeddingModel(IInferenceEngine engine)
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

    public async Task<float[]> GetEmbeddingAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        EnsureLoaded();
        var imageData = await PreprocessImageAsync(imageStream, cancellationToken);
        var result = await RunInferenceAsync(imageData, cancellationToken);
        return NormalizeEmbedding(result[0]);
    }

    public async Task<float[]> GetEmbeddingAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        EnsureLoaded();
        using var ms = new MemoryStream(imageData);
        return await GetEmbeddingAsync(ms, cancellationToken);
    }

    public async Task<float[][]> GetEmbeddingsBatchAsync(IEnumerable<byte[]> imagesData, CancellationToken cancellationToken = default)
    {
        EnsureLoaded();
        var images = imagesData.ToList();
        var batch = new float[images.Count][];

        var maxParallelism = _engine.Backend == ProcessingBackend.Cuda ? 1 : Environment.ProcessorCount;
        
        var tasks = images.Select(async (img, idx) =>
        {
            using var ms = new MemoryStream(img);
            var tensor = await PreprocessImageAsync(ms, cancellationToken);
            batch[idx] = tensor;
        });

        await Task.WhenAll(tasks);
        
        var result = await RunInferenceAsync(batch, cancellationToken);
        
        var embeddings = new float[images.Count][];
        for (int i = 0; i < images.Count; i++)
        {
            embeddings[i] = NormalizeEmbedding(result[i]);
        }
        
        return embeddings;
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
        using var image = await Image.LoadAsync<Bgra32>(imageStream, cancellationToken);
        
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(256, 256),
            Mode = ResizeMode.Crop,
            Position = AnchorPositionMode.Center
        }));

        var mean = new float[] { 0.48145466f, 0.4578275f, 0.40821073f };
        var std = new float[] { 0.26862954f, 0.26130258f, 0.27577711f };

        var input = new float[3 * 256 * 256];
        var idx = 0;
        
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < accessor.Width; x++)
                {
                    var pixel = row[x];
                    input[idx++] = (pixel.R / 255f - mean[0]) / std[0];
                    input[idx++] = (pixel.G / 255f - mean[1]) / std[1];
                    input[idx++] = (pixel.B / 255f - mean[2]) / std[2];
                }
            }
        });

        return input;
    }

    private async Task<float[][]> RunInferenceAsync(float[] input, CancellationToken cancellationToken)
    {
        var inputName = _session!.InputNames[0];
        var inputTensor = NamedOnnxValue.CreateFromTensor(inputName, new DenseTensor<float>(input, new[] { 1, 3, 256, 256 }));

        var inputs = new[] { inputTensor };
        var result = await _session!.RunAsync(inputs, cancellationToken);
        
        var outputs = new float[result.Outputs.Count][];
        for (int i = 0; i < result.Outputs.Count; i++)
        {
            var tensor = result.Outputs[i].AsTensor<float>();
            outputs[i] = tensor.ToArray();
        }
        
        return outputs;
    }

    private async Task<float[][]> RunInferenceAsync(float[][] batch, CancellationToken cancellationToken)
    {
        var inputName = _session!.InputNames[0];
        var flatBatch = batch.SelectMany(x => x).ToArray();
        var inputTensor = NamedOnnxValue.CreateFromTensor(inputName, new DenseTensor<float>(flatBatch, new[] { batch.Length, 3, 256, 256 }));

        var inputs = new[] { inputTensor };
        var result = await _session!.RunAsync(inputs, cancellationToken);
        
        var outputs = new float[result.Outputs.Count][];
        for (int i = 0; i < result.Outputs.Count; i++)
        {
            var tensor = result.Outputs[i].AsTensor<float>();
            outputs[i] = tensor.ToArray();
        }
        
        return outputs;
    }

    private float[] NormalizeEmbedding(float[] embedding)
    {
        var norm = Math.Sqrt(embedding.Sum(x => x * x));
        if (norm > 0)
        {
            for (int i = 0; i < embedding.Length; i++)
                embedding[i] /= (float)norm;
        }
        return embedding;
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