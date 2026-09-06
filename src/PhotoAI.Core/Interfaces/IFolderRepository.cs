using PhotoAI.Core.Models;

namespace PhotoAI.Core.Interfaces;

public interface IFolderRepository
{
    Task<Folder?> GetByIdAsync(long id);
    Task<Folder?> GetByPathAsync(string path);
    Task<IReadOnlyList<Folder>> GetAllAsync();
    Task<IReadOnlyList<Folder>> GetEnabledAsync();
    Task AddAsync(Folder folder);
    Task UpdateAsync(Folder folder);
    Task DeleteAsync(long id);
    Task<bool> ExistsByPathAsync(string path);
}
