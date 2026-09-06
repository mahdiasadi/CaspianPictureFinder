using PhotoAI.Core.Models;

namespace PhotoAI.Core.Interfaces;

public interface IAlbumRepository
{
    Task<Album?> GetByIdAsync(long id);
    Task<IReadOnlyList<Album>> GetAllAsync();
    Task<IReadOnlyList<Album>> GetSmartAlbumsAsync();
    Task AddAsync(Album album);
    Task UpdateAsync(Album album);
    Task DeleteAsync(long id);
    Task AddMediaToAlbumAsync(long albumId, long mediaItemId);
    Task RemoveMediaFromAlbumAsync(long albumId, long mediaItemId);
    Task<IReadOnlyList<MediaItem>> GetMediaByAlbumIdAsync(long albumId);
}
