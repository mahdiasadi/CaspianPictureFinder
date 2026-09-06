namespace PhotoAI.Core.Interfaces;

public interface IVectorStore : IDisposable
{
    Task InitializeAsync(string collectionName, int dimension, CancellationToken cancellationToken = default);
    Task UpsertAsync(string collectionName, long id, float[] vector, CancellationToken cancellationToken = default);
    Task UpsertBatchAsync(string collectionName, IReadOnlyList<(long Id, float[] Vector)> vectors, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(long Id, float Score)>> SearchAsync(string collectionName, float[] queryVector, int topK, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(long Id, float Score)>> SearchAsync(string collectionName, float[] queryVector, int topK, IReadOnlyList<long>? filterIds, CancellationToken cancellationToken = default);
    Task DeleteAsync(string collectionName, long id, CancellationToken cancellationToken = default);
    Task DeleteBatchAsync(string collectionName, IReadOnlyList<long> ids, CancellationToken cancellationToken = default);
    Task<long> GetCountAsync(string collectionName, CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);
}
