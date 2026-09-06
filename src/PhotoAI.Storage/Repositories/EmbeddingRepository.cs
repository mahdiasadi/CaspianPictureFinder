using Microsoft.EntityFrameworkCore;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Storage.Repositories;

public class EmbeddingRepository : IEmbeddingRepository
{
    private readonly Func<PhotoAiDbContext> _contextFactory;

    public EmbeddingRepository(Func<PhotoAiDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Embedding?> GetByIdAsync(long id)
    {
        using var context = _contextFactory();
        return await context.Embeddings.FindAsync(id);
    }

    public async Task<IReadOnlyList<Embedding>> GetByMediaItemIdAsync(long mediaItemId)
    {
        using var context = _contextFactory();
        return await context.Embeddings.Where(e => e.MediaItemId == mediaItemId).ToListAsync();
    }

    public async Task<IReadOnlyList<Embedding>> GetByModelAsync(string modelName, int skip, int take)
    {
        using var context = _contextFactory();
        return await context.Embeddings
            .Where(e => e.ModelName == modelName)
            .OrderBy(e => e.Id)
            .Skip(skip).Take(take)
            .ToListAsync();
    }

    public async Task<long> GetCountByModelAsync(string modelName)
    {
        using var context = _contextFactory();
        return await context.Embeddings.CountAsync(e => e.ModelName == modelName);
    }

    public async Task AddAsync(Embedding embedding)
    {
        using var context = _contextFactory();
        await context.Embeddings.AddAsync(embedding);
        await context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Embedding> embeddings)
    {
        using var context = _contextFactory();
        await context.Embeddings.AddRangeAsync(embeddings);
        await context.SaveChangesAsync();
    }

    public async Task DeleteByMediaItemIdAsync(long mediaItemId)
    {
        using var context = _contextFactory();
        var items = await context.Embeddings.Where(e => e.MediaItemId == mediaItemId).ToListAsync();
        context.Embeddings.RemoveRange(items);
        await context.SaveChangesAsync();
    }

    public async Task DeleteByModelAsync(string modelName)
    {
        using var context = _contextFactory();
        var items = await context.Embeddings.Where(e => e.ModelName == modelName).ToListAsync();
        context.Embeddings.RemoveRange(items);
        await context.SaveChangesAsync();
    }
}
