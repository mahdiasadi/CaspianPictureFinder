namespace PhotoAI.Core.Interfaces;

public interface ITextEmbeddingModel
{
    string ModelId { get; }
    string ModelName { get; }
    string Version { get; }
    int EmbeddingDimension { get; }
    bool IsLoaded { get; }

    Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    Task<float[][]> GetEmbeddingsBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
    Task LoadAsync(string modelPath, ProcessingBackend backend = ProcessingBackend.Auto, CancellationToken cancellationToken = default);
    void Unload();
}
