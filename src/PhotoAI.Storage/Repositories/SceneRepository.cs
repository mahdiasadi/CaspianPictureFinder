using Microsoft.EntityFrameworkCore;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Storage.Repositories;

public class SceneRepository : ISceneRepository
{
    private readonly Func<PhotoAiDbContext> _contextFactory;

    public SceneRepository(Func<PhotoAiDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<SceneLabel>> GetByMediaItemIdAsync(long mediaItemId)
    {
        using var context = _contextFactory();
        return await context.SceneLabels.Where(s => s.MediaItemId == mediaItemId).ToListAsync();
    }

    public async Task<IReadOnlyList<SceneLabel>> GetByLabelAsync(string label, int skip, int take)
    {
        using var context = _contextFactory();
        return await context.SceneLabels
            .Where(s => s.Label == label)
            .OrderByDescending(s => s.Confidence)
            .Skip(skip).Take(take)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<string>> GetAllLabelsAsync()
    {
        using var context = _contextFactory();
        return await context.SceneLabels.Select(s => s.Label).Distinct().ToListAsync();
    }

    public async Task<long> GetCountByLabelAsync(string label)
    {
        using var context = _contextFactory();
        return await context.SceneLabels.CountAsync(s => s.Label == label);
    }

    public async Task AddAsync(SceneLabel scene)
    {
        using var context = _contextFactory();
        await context.SceneLabels.AddAsync(scene);
        await context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<SceneLabel> scenes)
    {
        using var context = _contextFactory();
        await context.SceneLabels.AddRangeAsync(scenes);
        await context.SaveChangesAsync();
    }

    public async Task DeleteByMediaItemIdAsync(long mediaItemId)
    {
        using var context = _contextFactory();
        var items = await context.SceneLabels.Where(s => s.MediaItemId == mediaItemId).ToListAsync();
        context.SceneLabels.RemoveRange(items);
        await context.SaveChangesAsync();
    }
}
