using Microsoft.EntityFrameworkCore;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Storage.Repositories;

public class SettingsRepository : ISettingsRepository
{
    private readonly Func<PhotoAiDbContext> _contextFactory;

    public SettingsRepository(Func<PhotoAiDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<string?> GetAsync(string key)
    {
        using var context = _contextFactory();
        var setting = await context.Settings.FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value;
    }

    public async Task SetAsync(string key, string value, string? description = null)
    {
        using var context = _contextFactory();
        var setting = await context.Settings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting != null)
        {
            setting.Value = value;
            setting.Description = description ?? setting.Description;
            setting.DateModified = DateTime.UtcNow;
        }
        else
        {
            context.Settings.Add(new AppSettings
            {
                Key = key,
                Value = value,
                Description = description,
                DateModified = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();
    }

    public async Task<IDictionary<string, string>> GetAllAsync()
    {
        using var context = _contextFactory();
        var settings = await context.Settings.ToListAsync();
        return settings.ToDictionary(s => s.Key, s => s.Value);
    }

    public async Task DeleteAsync(string key)
    {
        using var context = _contextFactory();
        var setting = await context.Settings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting != null)
        {
            context.Settings.Remove(setting);
            await context.SaveChangesAsync();
        }
    }
}
