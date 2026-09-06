using Microsoft.EntityFrameworkCore;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Storage.Repositories;

public class MediaRepository : IMediaRepository
{
    private readonly Func<PhotoAiDbContext> _contextFactory;

    public MediaRepository(Func<PhotoAiDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<MediaItem?> GetByIdAsync(long id)
    {
        using var context = _contextFactory();
        return await context.MediaItems.FindAsync(id);
    }

    public async Task<MediaItem?> GetByPathAsync(string filePath)
    {
        using var context = _contextFactory();
        return await context.MediaItems.FirstOrDefaultAsync(m => m.FilePath == filePath);
    }

    public async Task<MediaItem?> GetByFilePathHashAsync(string filePathHash)
    {
        using var context = _contextFactory();
        return await context.MediaItems.FirstOrDefaultAsync(m => m.FilePathHash == filePathHash);
    }

    public async Task<IReadOnlyList<MediaItem>> GetByFolderIdAsync(long folderId)
    {
        using var context = _contextFactory();
        return await context.MediaItems.Where(m => m.FolderId == folderId).ToListAsync();
    }

    public async Task<IReadOnlyList<MediaItem>> GetAllAsync(int skip, int take)
    {
        using var context = _contextFactory();
        return await context.MediaItems
            .OrderByDescending(m => m.DateTaken ?? m.DateModified)
            .Skip(skip).Take(take)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<MediaItem>> GetByStatusAsync(MediaStatus status, int skip, int take)
    {
        using var context = _contextFactory();
        return await context.MediaItems
            .Where(m => m.Status == status)
            .Skip(skip).Take(take)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<MediaItem>> SearchByNameAsync(string query, int skip, int take)
    {
        using var context = _contextFactory();
        return await context.MediaItems
            .Where(m => m.FileName.Contains(query))
            .OrderByDescending(m => m.DateTaken ?? m.DateModified)
            .Skip(skip).Take(take)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<MediaItem>> GetByIdsAsync(IEnumerable<long> ids)
    {
        var idList = ids.ToList();
        if (!idList.Any()) return new List<MediaItem>();

        using var context = _contextFactory();
        return await context.MediaItems
            .Where(m => idList.Contains(m.Id))
            .ToListAsync();
    }

    public async Task<long> GetCountAsync()
    {
        using var context = _contextFactory();
        return await context.MediaItems.CountAsync();
    }

    public async Task<long> GetCountByStatusAsync(MediaStatus status)
    {
        using var context = _contextFactory();
        return await context.MediaItems.CountAsync(m => m.Status == status);
    }

    public async Task<long> GetCountByFolderAsync(long folderId)
    {
        using var context = _contextFactory();
        return await context.MediaItems.CountAsync(m => m.FolderId == folderId);
    }

    public async Task AddAsync(MediaItem mediaItem)
    {
        using var context = _contextFactory();
        await context.MediaItems.AddAsync(mediaItem);
        await context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<MediaItem> mediaItems)
    {
        using var context = _contextFactory();
        await context.MediaItems.AddRangeAsync(mediaItems);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(MediaItem mediaItem)
    {
        using var context = _contextFactory();
        context.MediaItems.Update(mediaItem);
        await context.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(long id, MediaStatus status, string? error = null)
    {
        using var context = _contextFactory();
        var item = await context.MediaItems.FindAsync(id);
        if (item != null)
        {
            item.Status = status;
            item.LastError = error;
            await context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(long id)
    {
        using var context = _contextFactory();
        var item = await context.MediaItems.FindAsync(id);
        if (item != null)
        {
            context.MediaItems.Remove(item);
            await context.SaveChangesAsync();
        }
    }

    public async Task DeleteByFolderIdAsync(long folderId)
    {
        using var context = _contextFactory();
        var items = await context.MediaItems.Where(m => m.FolderId == folderId).ToListAsync();
        context.MediaItems.RemoveRange(items);
        await context.SaveChangesAsync();
    }

    public async Task<bool> ExistsByPathAsync(string filePath)
    {
        using var context = _contextFactory();
        return await context.MediaItems.AnyAsync(m => m.FilePath == filePath);
    }

    public async Task<bool> ExistsByFilePathHashAsync(string filePathHash)
    {
        using var context = _contextFactory();
        return await context.MediaItems.AnyAsync(m => m.FilePathHash == filePathHash);
    }

    public async Task<int> GetNextIdAsync()
    {
        using var context = _contextFactory();
        var maxId = await context.MediaItems.MaxAsync(m => (int?)m.Id);
        return (maxId ?? 0) + 1;
    }
}
