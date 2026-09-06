using PhotoAI.Core.Models;

namespace PhotoAI.Core.Interfaces;

public interface IMediaRepository
{
    Task<MediaItem?> GetByIdAsync(long id);
    Task<MediaItem?> GetByPathAsync(string filePath);
    Task<MediaItem?> GetByFilePathHashAsync(string filePathHash);
    Task<IReadOnlyList<MediaItem>> GetByFolderIdAsync(long folderId);
    Task<IReadOnlyList<MediaItem>> GetAllAsync(int skip, int take);
    Task<IReadOnlyList<MediaItem>> GetByStatusAsync(MediaStatus status, int skip, int take);
    Task<IReadOnlyList<MediaItem>> SearchByNameAsync(string query, int skip, int take);
    Task<IReadOnlyList<MediaItem>> GetByIdsAsync(IEnumerable<long> ids);
    Task<long> GetCountAsync();
    Task<long> GetCountByStatusAsync(MediaStatus status);
    Task<long> GetCountByFolderAsync(long folderId);
    Task AddAsync(MediaItem mediaItem);
    Task AddRangeAsync(IEnumerable<MediaItem> mediaItems);
    Task UpdateAsync(MediaItem mediaItem);
    Task UpdateStatusAsync(long id, MediaStatus status, string? error = null);
    Task DeleteAsync(long id);
    Task DeleteByFolderIdAsync(long folderId);
    Task<bool> ExistsByPathAsync(string filePath);
    Task<bool> ExistsByFilePathHashAsync(string filePathHash);
    Task<int> GetNextIdAsync();
}
