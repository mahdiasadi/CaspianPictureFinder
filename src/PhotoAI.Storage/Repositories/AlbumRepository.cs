using Microsoft.EntityFrameworkCore;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Storage.Repositories;

public class AlbumRepository : IAlbumRepository
{
    private readonly Func<PhotoAiDbContext> _contextFactory;

    public AlbumRepository(Func<PhotoAiDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Album?> GetByIdAsync(long id)
    {
        using var context = _contextFactory();
        return await context.Albums.FindAsync(id);
    }

    public async Task<IReadOnlyList<Album>> GetAllAsync()
    {
        using var context = _contextFactory();
        return await context.Albums.OrderByDescending(a => a.DateModified ?? a.DateCreated).ToListAsync();
    }

    public async Task<IReadOnlyList<Album>> GetSmartAlbumsAsync()
    {
        using var context = _contextFactory();
        return await context.Albums.Where(a => a.IsSmartAlbum).ToListAsync();
    }

    public async Task AddAsync(Album album)
    {
        using var context = _contextFactory();
        await context.Albums.AddAsync(album);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Album album)
    {
        using var context = _contextFactory();
        context.Albums.Update(album);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        using var context = _contextFactory();
        var album = await context.Albums.FindAsync(id);
        if (album != null)
        {
            context.Albums.Remove(album);
            await context.SaveChangesAsync();
        }
    }

    public async Task AddMediaToAlbumAsync(long albumId, long mediaItemId)
    {
        using var context = _contextFactory();
        var exists = await context.AlbumMediaItems.AnyAsync(am => am.AlbumId == albumId && am.MediaItemId == mediaItemId);
        if (!exists)
        {
            context.AlbumMediaItems.Add(new AlbumMedia
            {
                AlbumId = albumId,
                MediaItemId = mediaItemId,
                DateAdded = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }
    }

    public async Task RemoveMediaFromAlbumAsync(long albumId, long mediaItemId)
    {
        using var context = _contextFactory();
        var item = await context.AlbumMediaItems.FirstOrDefaultAsync(am => am.AlbumId == albumId && am.MediaItemId == mediaItemId);
        if (item != null)
        {
            context.AlbumMediaItems.Remove(item);
            await context.SaveChangesAsync();
        }
    }

    public async Task<IReadOnlyList<MediaItem>> GetMediaByAlbumIdAsync(long albumId)
    {
        using var context = _contextFactory();
        return await context.AlbumMediaItems
            .Where(am => am.AlbumId == albumId)
            .Select(am => am.MediaItem)
            .ToListAsync();
    }
}
