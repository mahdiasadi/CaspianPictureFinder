using PhotoAI.Core.Models;

namespace PhotoAI.Core.Interfaces;

public interface IIndexJobRepository
{
    Task<IndexJob?> GetByIdAsync(long id);
    Task<IndexJob?> GetRunningJobAsync();
    Task<IReadOnlyList<IndexJob>> GetAllAsync(int skip, int take);
    Task AddAsync(IndexJob job);
    Task UpdateAsync(IndexJob job);
    Task DeleteAsync(long id);
}
