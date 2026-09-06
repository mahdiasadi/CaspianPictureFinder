using PhotoAI.Core.Models;

namespace PhotoAI.Core.Interfaces;

public interface ISceneRepository
{
    Task<IReadOnlyList<SceneLabel>> GetByMediaItemIdAsync(long mediaItemId);
    Task<IReadOnlyList<SceneLabel>> GetByLabelAsync(string label, int skip, int take);
    Task<IReadOnlyList<string>> GetAllLabelsAsync();
    Task<long> GetCountByLabelAsync(string label);
    Task AddAsync(SceneLabel scene);
    Task AddRangeAsync(IEnumerable<SceneLabel> scenes);
    Task DeleteByMediaItemIdAsync(long mediaItemId);
}
