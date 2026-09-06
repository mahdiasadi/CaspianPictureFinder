using Microsoft.EntityFrameworkCore;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Storage.Repositories;

public class FolderRepository : IFolderRepository
{
    private readonly Func<PhotoAiDbContext> _contextFactory;

    public FolderRepository(Func<PhotoAiDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Folder?> GetByIdAsync(long id)
    {
        using var context = _contextFactory();
        return await context.Folders.FindAsync(id);
    }

    public async Task<Folder?> GetByPathAsync(string path)
    {
        using var context = _contextFactory();
        return await context.Folders.FirstOrDefaultAsync(f => f.Path == path);
    }

    public async Task<IReadOnlyList<Folder>> GetAllAsync()
    {
        using var context = _contextFactory();
        return await context.Folders.OrderByDescending(f => f.DateAdded).ToListAsync();
    }

    public async Task<IReadOnlyList<Folder>> GetEnabledAsync()
    {
        using var context = _contextFactory();
        return await context.Folders.Where(f => f.IsEnabled).ToListAsync();
    }

    public async Task AddAsync(Folder folder)
    {
        using var context = _contextFactory();
        await context.Folders.AddAsync(folder);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Folder folder)
    {
        using var context = _contextFactory();
        context.Folders.Update(folder);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        using var context = _contextFactory();
        var folder = await context.Folders.FindAsync(id);
        if (folder != null)
        {
            context.Folders.Remove(folder);
            await context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsByPathAsync(string path)
    {
        using var context = _contextFactory();
        return await context.Folders.AnyAsync(f => f.Path == path);
    }
}
