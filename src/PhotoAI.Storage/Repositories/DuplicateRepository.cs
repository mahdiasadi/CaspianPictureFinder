using Microsoft.EntityFrameworkCore;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Storage.Repositories;

public class DuplicateRepository : IDuplicateRepository
{
    private readonly Func<PhotoAiDbContext> _contextFactory;

    public DuplicateRepository(Func<PhotoAiDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<DuplicateGroup>> GetAllGroupsAsync(int skip, int take)
    {
        using var context = _contextFactory();
        return await context.DuplicateGroups
            .OrderByDescending(g => g.SimilarityScore)
            .Skip(skip).Take(take)
            .ToListAsync();
    }

    public async Task<DuplicateGroup?> GetGroupByIdAsync(long id)
    {
        using var context = _contextFactory();
        return await context.DuplicateGroups.FindAsync(id);
    }

    public async Task<IReadOnlyList<DuplicateEntry>> GetEntriesByGroupIdAsync(long groupId)
    {
        using var context = _contextFactory();
        return await context.DuplicateEntries
            .Where(e => e.DuplicateGroupId == groupId)
            .ToListAsync();
    }

    public async Task<long> GetGroupCountAsync()
    {
        using var context = _contextFactory();
        return await context.DuplicateGroups.CountAsync();
    }

    public async Task AddGroupAsync(DuplicateGroup group)
    {
        using var context = _contextFactory();
        await context.DuplicateGroups.AddAsync(group);
        await context.SaveChangesAsync();
    }

    public async Task AddEntriesAsync(IEnumerable<DuplicateEntry> entries)
    {
        using var context = _contextFactory();
        await context.DuplicateEntries.AddRangeAsync(entries);
        await context.SaveChangesAsync();
    }

    public async Task DeleteGroupAsync(long groupId)
    {
        using var context = _contextFactory();
        var entries = await context.DuplicateEntries.Where(e => e.DuplicateGroupId == groupId).ToListAsync();
        context.DuplicateEntries.RemoveRange(entries);
        var group = await context.DuplicateGroups.FindAsync(groupId);
        if (group != null)
        {
            context.DuplicateGroups.Remove(group);
        }
        await context.SaveChangesAsync();
    }

    public async Task UpdateEntryAsync(DuplicateEntry entry)
    {
        using var context = _contextFactory();
        context.DuplicateEntries.Update(entry);
        await context.SaveChangesAsync();
    }
}
