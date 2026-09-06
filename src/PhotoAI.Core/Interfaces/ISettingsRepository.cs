using PhotoAI.Core.Models;

namespace PhotoAI.Core.Interfaces;

public interface ISettingsRepository
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value, string? description = null);
    Task<IDictionary<string, string>> GetAllAsync();
    Task DeleteAsync(string key);
}
