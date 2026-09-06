using PhotoAI.Core.Models;

namespace PhotoAI.Core.Interfaces;

public interface ITagRepository
{
    Task<IReadOnlyList<MediaTag>> GetByMediaItemIdAsync(long mediaItemId);
    Task<IReadOnlyList<MediaTag>> GetByTagAsync(string tag, int skip, int take);
    Task<IReadOnlyList<string>> GetAllTagsAsync();
    Task AddAsync(MediaTag tag);
    Task AddRangeAsync(IEnumerable<MediaTag> tags);
    Task DeleteAsync(long id);
    Task DeleteByMediaItemIdAsync(long mediaItemId);
}
