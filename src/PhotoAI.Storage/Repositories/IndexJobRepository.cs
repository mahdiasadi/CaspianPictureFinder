using Microsoft.EntityFrameworkCore;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Storage.Repositories;

public class IndexJobRepository : IIndexJobRepository
{
    private readonly Func<PhotoAiDbContext> _contextFactory;

    public IndexJobRepository(Func<PhotoAiDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IndexJob?> GetByIdAsync(long id)
    {
        using var context = _contextFactory();
        return await context.IndexJobs.FindAsync(id);
    }

    public async Task<IndexJob?> GetRunningJobAsync()
    {
        using var context = _contextFactory();
        return await context.IndexJobs.FirstOrDefaultAsync(j => j.Status == IndexJobStatus.Running);
    }

    public async Task<IReadOnlyList<IndexJob>> GetAllAsync(int skip, int take)
    {
        using var context = _contextFactory();
        return await context.IndexJobs
            .OrderByDescending(j => j.DateStarted)
            .Skip(skip).Take(take)
            .ToListAsync();
    }

    public async Task AddAsync(IndexJob job)
    {
        using var context = _contextFactory();
        await context.IndexJobs.AddAsync(job);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(IndexJob job)
    {
        using var context = _contextFactory();
        context.IndexJobs.Update(job);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        using var context = _contextFactory();
        var job = await context.IndexJobs.FindAsync(id);
        if (job != null)
        {
            context.IndexJobs.Remove(job);
            await context.SaveChangesAsync();
        }
    }
}
