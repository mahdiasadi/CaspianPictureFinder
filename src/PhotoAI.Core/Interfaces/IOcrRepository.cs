using PhotoAI.Core.Models;

namespace PhotoAI.Core.Interfaces;

public interface IOcrRepository
{
    Task<IReadOnlyList<OcrResult>> GetByMediaItemIdAsync(long mediaItemId);
    Task<IReadOnlyList<OcrResult>> GetByTextAsync(string text, int skip, int take);
    Task<long> GetCountAsync();
    Task AddAsync(OcrResult ocrResult);
    Task AddRangeAsync(IEnumerable<OcrResult> ocrResults);
    Task DeleteByMediaItemIdAsync(long mediaItemId);
}