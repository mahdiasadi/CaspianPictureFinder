namespace PhotoAI.Core.Interfaces;

public interface IImageEmbeddingModel
{
    string ModelId { get; }
    string ModelName { get; }
    string Version { get; }
    int EmbeddingDimension { get; }
    bool IsLoaded { get; }

    Task<float[]> GetEmbeddingAsync(Stream imageStream, CancellationToken cancellationToken = default);
    Task<float[]> GetEmbeddingAsync(byte[] imageData, CancellationToken cancellationToken = default);
    Task<float[][]> GetEmbeddingsBatchAsync(IEnumerable<byte[]> imagesData, CancellationToken cancellationToken = default);
    Task LoadAsync(string modelPath, ProcessingBackend backend = ProcessingBackend.Auto, CancellationToken cancellationToken = default);
    void Unload();
}
