using Microsoft.EntityFrameworkCore;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Storage.Repositories;

public class ObjectDetectionRepository : IObjectDetectionRepository
{
    private readonly Func<PhotoAiDbContext> _contextFactory;

    public ObjectDetectionRepository(Func<PhotoAiDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<ObjectDetection>> GetByMediaItemIdAsync(long mediaItemId)
    {
        using var context = _contextFactory();
        return await context.ObjectDetections.Where(o => o.MediaItemId == mediaItemId).ToListAsync();
    }

    public async Task<IReadOnlyList<ObjectDetection>> GetByLabelAsync(string label, int skip, int take)
    {
        using var context = _contextFactory();
        return await context.ObjectDetections
            .Where(o => o.Label == label)
            .OrderByDescending(o => o.Confidence)
            .Skip(skip).Take(take)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<string>> GetAllLabelsAsync()
    {
        using var context = _contextFactory();
        return await context.ObjectDetections.Select(o => o.Label).Distinct().ToListAsync();
    }

    public async Task<long> GetCountByLabelAsync(string label)
    {
        using var context = _contextFactory();
        return await context.ObjectDetections.CountAsync(o => o.Label == label);
    }

    public async Task AddAsync(ObjectDetection detection)
    {
        using var context = _contextFactory();
        await context.ObjectDetections.AddAsync(detection);
        await context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<ObjectDetection> detections)
    {
        using var context = _contextFactory();
        await context.ObjectDetections.AddRangeAsync(detections);
        await context.SaveChangesAsync();
    }

    public async Task DeleteByMediaItemIdAsync(long mediaItemId)
    {
        using var context = _contextFactory();
        var items = await context.ObjectDetections.Where(o => o.MediaItemId == mediaItemId).ToListAsync();
        context.ObjectDetections.RemoveRange(items);
        await context.SaveChangesAsync();
    }
}
