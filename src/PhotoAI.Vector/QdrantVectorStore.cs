// Qdrant Vector Store Implementation
// Requires Qdrant.Client NuGet package
// Uncomment and install Qdrant.Client NuGet package to use

/*
using Qdrant.Client;
using Qdrant.Client.Grpc;
using PhotoAI.Core.Interfaces;

namespace PhotoAI.Vector;

public class QdrantVectorStore : IVectorStore
{
    private readonly QdrantClient _client;
    private readonly string _collectionPrefix;
    private readonly Dictionary<string, int> _collectionDimensions = new();
    private bool _disposed;

    public QdrantVectorStore(string host = "localhost", int port = 6334, string? apiKey = null, string collectionPrefix = "photoai")
    {
        _collectionPrefix = collectionPrefix;
        _client = new QdrantClient(host, port, apiKey: apiKey, useHttps: false);
    }

    public async Task InitializeAsync(string collectionName, int dimension, CancellationToken cancellationToken = default)
    {
        var fullName = $"{_collectionPrefix}_{collectionName}";
        
        if (!_collectionDimensions.ContainsKey(fullName))
        {
            var exists = await _client.CollectionExistsAsync(fullName, cancellationToken: cancellationToken);
            if (!exists)
            {
                await _client.CreateCollectionAsync(fullName, new VectorParams
                {
                    Size = (ulong)dimension,
                    Distance = Distance.Cosine
                }, cancellationToken: cancellationToken);
            }
            _collectionDimensions[fullName] = dimension;
        }
    }

    public async Task UpsertAsync(string collectionName, long id, float[] vector, CancellationToken cancellationToken = default)
    {
        await UpsertBatchAsync(collectionName, new[] { (id, vector) }, cancellationToken);
    }

    public async Task UpsertBatchAsync(string collectionName, IReadOnlyList<(long Id, float[] Vector)> vectors, CancellationToken cancellationToken = default)
    {
        var fullName = $"{_collectionPrefix}_{collectionName}";
        await InitializeAsync(collectionName, vectors[0].Vector.Length, cancellationToken);

        var points = vectors.Select(v => new PointStruct
        {
            Id = v.Id,
            Vectors = v.Vector.Select(f => (double)f).ToArray()
        }).ToList();

        await _client.UpsertAsync(fullName, points, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<(long Id, float Score)>> SearchAsync(string collectionName, float[] queryVector, int topK, CancellationToken cancellationToken = default)
    {
        return await SearchAsync(collectionName, queryVector, topK, null, cancellationToken);
    }

    public async Task<IReadOnlyList<(long Id, float Score)>> SearchAsync(string collectionName, float[] queryVector, int topK, IReadOnlyList<long>? filterIds, CancellationToken cancellationToken = default)
    {
        var fullName = $"{_collectionPrefix}_{collectionName}";
        
        var filter = filterIds?.Count > 0 
            ? new Filter { Must = { new FieldCondition { Key = "id", Match = new Match { Any = filterIds.Select(id => (Value)id).ToArray() } } } }
            : null;

        var results = await _client.SearchAsync(fullName, queryVector.Select(f => (double)f).ToArray(), topK, filter: filter, cancellationToken: cancellationToken);
        
        return results.Select(r => (r.Id.Num, (float)r.Score)).ToList();
    }

    public async Task DeleteAsync(string collectionName, long id, CancellationToken cancellationToken = default)
    {
        var fullName = $"{_collectionPrefix}_{collectionName}";
        await _client.DeleteAsync(fullName, new[] { id }, cancellationToken: cancellationToken);
    }

    public async Task DeleteBatchAsync(string collectionName, IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
    {
        var fullName = $"{_collectionPrefix}_{collectionName}";
        await _client.DeleteAsync(fullName, ids, cancellationToken: cancellationToken);
    }

    public async Task<long> GetCountAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        var fullName = $"{_collectionPrefix}_{collectionName}";
        var info = await _client.GetCollectionInfoAsync(fullName, cancellationToken: cancellationToken);
        return info.Result.VectorsCount;
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _client?.Dispose();
            _disposed = true;
        }
    }
}
*/