using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using PhotoAI.Core.Inference;
using PhotoAI.Core.Interfaces;

namespace PhotoAI.AI.Vision;

public class SigLipTextEmbeddingModel : ITextEmbeddingModel
{
    private readonly IInferenceEngine _engine;
    private IInferenceSession? _session;
    private readonly Dictionary<string, int> _vocab = new();
    private bool _disposed;

    public string ModelId => "siglip2-so400m-patch16-256-text";
    public string ModelName => "SigLIP2 SO400M Text Encoder";
    public string Version => "1.0";
    public int EmbeddingDimension => 768;
    public bool IsLoaded => _session != null;

    public SigLipTextEmbeddingModel(IInferenceEngine engine)
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
        
        LoadVocab();
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        EnsureLoaded();
        var tokens = Tokenize(text);
        var result = await RunInferenceAsync(tokens, cancellationToken);
        return NormalizeEmbedding(result[0]);
    }

    public async Task<float[][]> GetEmbeddingsBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        EnsureLoaded();
        var textList = texts.ToList();
        var embeddings = new float[textList.Count][];

        var batchTokens = textList.Select(Tokenize).ToArray();
        var result = await RunBatchInferenceAsync(batchTokens, cancellationToken);

        for (int i = 0; i < textList.Count; i++)
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

    private void LoadVocab()
    {
        for (int i = 0; i < 49408; i++)
        {
            _vocab[i.ToString()] = i;
        }
        _vocab["<|endoftext|>"] = 49407;
    }

    private int[] Tokenize(string text)
    {
        var words = text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var tokens = new List<int> { 49406 };
        
        foreach (var word in words)
        {
            if (_vocab.TryGetValue(word, out var id))
                tokens.Add(id);
            else
                tokens.Add(49407);
        }
        
        tokens.Add(49407);
        
        if (tokens.Count > 77)
            tokens = tokens.Take(77).ToList();
        else
            tokens.AddRange(Enumerable.Repeat(0, 77 - tokens.Count));
        
        return tokens.ToArray();
    }

    private async Task<float[][]> RunInferenceAsync(int[] tokens, CancellationToken cancellationToken)
    {
        var inputName = _session!.InputNames[0];
        var inputTensor = NamedOnnxValue.CreateFromTensor(inputName, new DenseTensor<long>(tokens.Select(t => (long)t).ToArray(), new[] { 1, 77 }));

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

    private async Task<float[][]> RunBatchInferenceAsync(int[][] batchTokens, CancellationToken cancellationToken)
    {
        var inputName = _session!.InputNames[0];
        var flatTokens = batchTokens.SelectMany(x => x).Select(t => (long)t).ToArray();
        var inputTensor = NamedOnnxValue.CreateFromTensor(inputName, new DenseTensor<long>(flatTokens, new[] { batchTokens.Length, 77 }));

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