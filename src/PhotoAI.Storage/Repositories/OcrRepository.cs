using Microsoft.EntityFrameworkCore;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Storage.Repositories;

public class OcrRepository : IOcrRepository
{
    private readonly Func<PhotoAiDbContext> _contextFactory;

    public OcrRepository(Func<PhotoAiDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<OcrResult>> GetByMediaItemIdAsync(long mediaItemId)
    {
        using var context = _contextFactory();
        return await context.OcrResults.Where(o => o.MediaItemId == mediaItemId).ToListAsync();
    }

    public async Task<IReadOnlyList<OcrResult>> GetByTextAsync(string text, int skip, int take)
    {
        using var context = _contextFactory();
        return await context.OcrResults
            .Where(o => o.Text.Contains(text))
            .OrderByDescending(o => o.Confidence)
            .Skip(skip).Take(take)
            .ToListAsync();
    }

    public async Task<long> GetCountAsync()
    {
        using var context = _contextFactory();
        return await context.OcrResults.CountAsync();
    }

    public async Task AddAsync(OcrResult ocrResult)
    {
        using var context = _contextFactory();
        await context.OcrResults.AddAsync(ocrResult);
        await context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<OcrResult> ocrResults)
    {
        using var context = _contextFactory();
        await context.OcrResults.AddRangeAsync(ocrResults);
        await context.SaveChangesAsync();
    }

    public async Task DeleteByMediaItemIdAsync(long mediaItemId)
    {
        using var context = _contextFactory();
        var items = await context.OcrResults.Where(o => o.MediaItemId == mediaItemId).ToListAsync();
        context.OcrResults.RemoveRange(items);
        await context.SaveChangesAsync();
    }
}