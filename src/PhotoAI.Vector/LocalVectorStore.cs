using System.Diagnostics;
using System.Runtime.CompilerServices;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Vector;

public class LocalVectorStore : IVectorStore
{
    private readonly string _indexDirectory;
    private readonly Dictionary<string, HnswIndex> _indexes = new();
    private readonly object _lock = new();
    private bool _disposed;

    public LocalVectorStore(string indexDirectory)
    {
        _indexDirectory = indexDirectory;
        Directory.CreateDirectory(_indexDirectory);
    }

    public async Task InitializeAsync(string collectionName, int dimension, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                if (!_indexes.ContainsKey(collectionName))
                {
                    var indexPath = Path.Combine(_indexDirectory, $"{collectionName}.hnsw");
                    _indexes[collectionName] = new HnswIndex(indexPath, dimension);
                }
            }
        }, cancellationToken);
    }

    public async Task UpsertAsync(string collectionName, long id, float[] vector, CancellationToken cancellationToken = default)
    {
        var index = GetOrCreateIndex(collectionName, vector.Length);
        await Task.Run(() => index.Upsert(id, vector), cancellationToken);
    }

    public async Task UpsertBatchAsync(string collectionName, IReadOnlyList<(long Id, float[] Vector)> vectors, CancellationToken cancellationToken = default)
    {
        var index = GetOrCreateIndex(collectionName, vectors[0].Vector.Length);
        await Task.Run(() =>
        {
            foreach (var (id, vector) in vectors)
            {
                index.Upsert(id, vector);
            }
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<(long Id, float Score)>> SearchAsync(string collectionName, float[] queryVector, int topK, CancellationToken cancellationToken = default)
    {
        var index = GetOrCreateIndex(collectionName, queryVector.Length);
        return await Task.Run(() => index.Search(queryVector, topK), cancellationToken);
    }

    public async Task<IReadOnlyList<(long Id, float Score)>> SearchAsync(string collectionName, float[] queryVector, int topK, IReadOnlyList<long>? filterIds, CancellationToken cancellationToken = default)
    {
        var index = GetOrCreateIndex(collectionName, queryVector.Length);
        return await Task.Run(() => index.Search(queryVector, topK, filterIds), cancellationToken);
    }

    public async Task DeleteAsync(string collectionName, long id, CancellationToken cancellationToken = default)
    {
        if (_indexes.TryGetValue(collectionName, out var index))
        {
            await Task.Run(() => index.Delete(id), cancellationToken);
        }
    }

    public async Task DeleteBatchAsync(string collectionName, IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
    {
        if (_indexes.TryGetValue(collectionName, out var index))
        {
            await Task.Run(() =>
            {
                foreach (var id in ids)
                {
                    index.Delete(id);
                }
            }, cancellationToken);
        }
    }

    public async Task<long> GetCountAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        if (_indexes.TryGetValue(collectionName, out var index))
        {
            return await Task.Run(() => index.Count, cancellationToken);
        }
        return 0;
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                foreach (var index in _indexes.Values)
                {
                    index.Save();
                }
            }
        }, cancellationToken);
    }

    private HnswIndex GetOrCreateIndex(string collectionName, int dimension)
    {
        lock (_lock)
        {
            if (!_indexes.TryGetValue(collectionName, out var index))
            {
                var indexPath = Path.Combine(_indexDirectory, $"{collectionName}.hnsw");
                index = new HnswIndex(indexPath, dimension);
                _indexes[collectionName] = index;
            }
            return index;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            lock (_lock)
            {
                foreach (var index in _indexes.Values)
                {
                    index.Dispose();
                }
                _indexes.Clear();
                _disposed = true;
            }
        }
    }
}

internal class HnswIndex : IDisposable
{
    private readonly int _dimension;
    private readonly string _indexPath;
    private readonly Dictionary<long, float[]> _vectors = new();
    private readonly object _lock = new();
    private bool _disposed;
    private bool _dirty;

    public long Count => _vectors.Count;

    public HnswIndex(string indexPath, int dimension)
    {
        _indexPath = indexPath;
        _dimension = dimension;
        Load();
    }

    public void Upsert(long id, float[] vector)
    {
        if (vector.Length != _dimension)
            throw new ArgumentException($"Vector dimension mismatch: expected {_dimension}, got {vector.Length}");

        lock (_lock)
        {
            _vectors[id] = Normalize(vector);
            _dirty = true;
        }
    }

    public IReadOnlyList<(long Id, float Score)> Search(float[] queryVector, int topK, IReadOnlyList<long>? filterIds = null)
    {
        var normalizedQuery = Normalize(queryVector);
        
        lock (_lock)
        {
            var candidates = _vectors.AsEnumerable();
            
            if (filterIds != null && filterIds.Count > 0)
            {
                var filterSet = new HashSet<long>(filterIds);
                candidates = candidates.Where(kvp => filterSet.Contains(kvp.Key));
            }

            var results = candidates
                .Select(kvp => (kvp.Key, Score: CosineSimilarity(normalizedQuery, kvp.Value)))
                .OrderByDescending(x => x.Score)
                .Take(topK)
                .ToList();

            return results;
        }
    }

    public void Delete(long id)
    {
        lock (_lock)
        {
            _vectors.Remove(id);
            _dirty = true;
        }
    }

    public void Save()
    {
        lock (_lock)
        {
            if (!_dirty) return;

            var dir = Path.GetDirectoryName(_indexPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            using var stream = new FileStream(_indexPath + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new BinaryWriter(stream);

            writer.Write(_dimension);
            writer.Write(_vectors.Count);

            foreach (var (id, vector) in _vectors)
            {
                writer.Write(id);
                writer.Write(vector.Length);
                foreach (var v in vector)
                    writer.Write(v);
            }

            writer.Flush();
            stream.Close();

            if (File.Exists(_indexPath))
                File.Delete(_indexPath);
            
            File.Move(_indexPath + ".tmp", _indexPath);
            _dirty = false;
        }
    }

    private void Load()
    {
        if (!File.Exists(_indexPath)) return;

        try
        {
            using var stream = new FileStream(_indexPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(stream);

            var dim = reader.ReadInt32();
            if (dim != _dimension)
            {
                // Dimension mismatch, start fresh
                return;
            }

            var count = reader.ReadInt64();
            for (long i = 0; i < count; i++)
            {
                var id = reader.ReadInt64();
                var vecDim = reader.ReadInt32();
                var vector = new float[vecDim];
                for (int j = 0; j < vecDim; j++)
                {
                    vector[j] = reader.ReadSingle();
                }
                _vectors[id] = vector;
            }
        }
        catch
        {
            // Corrupted index, start fresh
            _vectors.Clear();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float[] Normalize(float[] vector)
    {
        var norm = 0.0;
        for (int i = 0; i < vector.Length; i++)
            norm += vector[i] * vector[i];
        
        norm = Math.Sqrt(norm);
        if (norm > 0)
        {
            for (int i = 0; i < vector.Length; i++)
                vector[i] /= (float)norm;
        }
        return vector;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float CosineSimilarity(float[] a, float[] b)
    {
        float dot = 0;
        for (int i = 0; i < a.Length; i++)
            dot += a[i] * b[i];
        return dot; // Already normalized
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Save();
            _disposed = true;
        }
    }
}