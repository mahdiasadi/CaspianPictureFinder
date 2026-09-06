using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using PhotoAI.Core.Inference;
using PhotoAI.Core.Interfaces;

namespace PhotoAI.AI.Face;

public class SFaceEmbeddingModel : IFaceEmbeddingModel
{
    private readonly IInferenceEngine _engine;
    private IInferenceSession? _session;
    private bool _disposed;

    public string ModelId => "sface";
    public string ModelName => "SFace";
    public string Version => "1.0";
    public int EmbeddingDimension => 128;
    public bool IsLoaded => _session != null;

    private const int InputSize = 112;

    public SFaceEmbeddingModel(IInferenceEngine engine)
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

    public async Task<float[]> GetEmbeddingAsync(byte[] faceImageData, CancellationToken cancellationToken = default)
    {
        EnsureLoaded();
        using var ms = new MemoryStream(faceImageData);
        return await GetEmbeddingAsync(ms, cancellationToken);
    }

    public async Task<float[]> GetEmbeddingAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        EnsureLoaded();
        
        var preprocessedTensor = await PreprocessImageAsync(imageStream, cancellationToken);
        
        var inputName = _session!.InputNames[0];
        var inputTensor = NamedOnnxValue.CreateFromTensor(
            _session.InputNames[0], 
            new DenseTensor<float>(preprocessedTensor, new[] { 1, 3, InputSize, InputSize }));
        
        var inputs = new[] { inputTensor };
        var result = await _session.RunAsync(inputs, cancellationToken);
        
        var outputTensor = result.Outputs[0].AsTensor<float>();
        var embedding = outputTensor.ToArray();
        
        return NormalizeEmbedding(embedding);
    }

    public async Task<float[][]> GetEmbeddingsBatchAsync(IEnumerable<byte[]> faceImagesData, CancellationToken cancellationToken = default)
    {
        EnsureLoaded();
        var images = faceImagesData.ToList();
        var embeddings = new float[images.Count][];

        // Process in parallel for CPU, sequentially for GPU
        var maxParallelism = _engine.Backend == ProcessingBackend.Cuda ? 1 : Environment.ProcessorCount;
        
        var tasks = images.Select(async (img, idx) =>
        {
            using var ms = new MemoryStream(img);
            embeddings[idx] = await GetEmbeddingAsync(ms, cancellationToken);
        });

        await Task.WhenAll(tasks);
        return embeddings;
    }

    public Task<float> ComputeSimilarityAsync(float[] embedding1, float[] embedding2)
    {
        // Cosine similarity (embeddings are already L2 normalized)
        float dot = 0;
        for (int i = 0; i < embedding1.Length; i++)
            dot += embedding1[i] * embedding2[i];
        
        return Task.FromResult(dot);
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
        
        // Resize to 112x112 with center crop
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(InputSize, InputSize),
            Mode = ResizeMode.Crop,
            Position = AnchorPositionMode.Center
        }));

        // Normalize: (pixel - 127.5) / 127.5 to [-1, 1]
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
                            0 => pixel.R,
                            1 => pixel.G,
                            _ => pixel.B
                        };
                        tensor[idx++] = (val - 127.5f) / 127.5f;
                    }
                }
            }
        });

        return tensor;
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