using Microsoft.EntityFrameworkCore;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Storage.Repositories;

public class TagRepository : ITagRepository
{
    private readonly Func<PhotoAiDbContext> _contextFactory;

    public TagRepository(Func<PhotoAiDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<MediaTag>> GetByMediaItemIdAsync(long mediaItemId)
    {
        using var context = _contextFactory();
        return await context.MediaTags.Where(t => t.MediaItemId == mediaItemId).ToListAsync();
    }

    public async Task<IReadOnlyList<MediaTag>> GetByTagAsync(string tag, int skip, int take)
    {
        using var context = _contextFactory();
        return await context.MediaTags
            .Where(t => t.Tag == tag)
            .OrderByDescending(t => t.DateCreated)
            .Skip(skip).Take(take)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<string>> GetAllTagsAsync()
    {
        using var context = _contextFactory();
        return await context.MediaTags.Select(t => t.Tag).Distinct().ToListAsync();
    }

    public async Task AddAsync(MediaTag tag)
    {
        using var context = _contextFactory();
        await context.MediaTags.AddAsync(tag);
        await context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<MediaTag> tags)
    {
        using var context = _contextFactory();
        await context.MediaTags.AddRangeAsync(tags);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        using var context = _contextFactory();
        var tag = await context.MediaTags.FindAsync(id);
        if (tag != null)
        {
            context.MediaTags.Remove(tag);
            await context.SaveChangesAsync();
        }
    }

    public async Task DeleteByMediaItemIdAsync(long mediaItemId)
    {
        using var context = _contextFactory();
        var tags = await context.MediaTags.Where(t => t.MediaItemId == mediaItemId).ToListAsync();
        context.MediaTags.RemoveRange(tags);
        await context.SaveChangesAsync();
    }
}
