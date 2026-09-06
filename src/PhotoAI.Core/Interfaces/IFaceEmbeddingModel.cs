namespace PhotoAI.Core.Interfaces;

public interface IFaceEmbeddingModel
{
    string ModelId { get; }
    string ModelName { get; }
    string Version { get; }
    int EmbeddingDimension { get; }
    bool IsLoaded { get; }

    Task<float[]> GetEmbeddingAsync(byte[] faceImageData, CancellationToken cancellationToken = default);
    Task<float[][]> GetEmbeddingsBatchAsync(IEnumerable<byte[]> faceImagesData, CancellationToken cancellationToken = default);
    Task<float> ComputeSimilarityAsync(float[] embedding1, float[] embedding2);
    Task LoadAsync(string modelPath, ProcessingBackend backend = ProcessingBackend.Auto, CancellationToken cancellationToken = default);
    void Unload();
}
