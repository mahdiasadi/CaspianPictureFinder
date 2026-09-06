using PhotoAI.Core.Models;

namespace PhotoAI.Core.Interfaces;

public interface IEmbeddingRepository
{
    Task<Embedding?> GetByIdAsync(long id);
    Task<IReadOnlyList<Embedding>> GetByMediaItemIdAsync(long mediaItemId);
    Task<IReadOnlyList<Embedding>> GetByModelAsync(string modelName, int skip, int take);
    Task<long> GetCountByModelAsync(string modelName);
    Task AddAsync(Embedding embedding);
    Task AddRangeAsync(IEnumerable<Embedding> embeddings);
    Task DeleteByMediaItemIdAsync(long mediaItemId);
    Task DeleteByModelAsync(string modelName);
}
