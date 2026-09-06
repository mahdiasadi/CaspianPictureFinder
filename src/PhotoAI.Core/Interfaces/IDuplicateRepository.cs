using PhotoAI.Core.Models;

namespace PhotoAI.Core.Interfaces;

public interface IDuplicateRepository
{
    Task<IReadOnlyList<DuplicateGroup>> GetAllGroupsAsync(int skip, int take);
    Task<DuplicateGroup?> GetGroupByIdAsync(long id);
    Task<IReadOnlyList<DuplicateEntry>> GetEntriesByGroupIdAsync(long groupId);
    Task<long> GetGroupCountAsync();
    Task AddGroupAsync(DuplicateGroup group);
    Task AddEntriesAsync(IEnumerable<DuplicateEntry> entries);
    Task DeleteGroupAsync(long groupId);
    Task UpdateEntryAsync(DuplicateEntry entry);
}
