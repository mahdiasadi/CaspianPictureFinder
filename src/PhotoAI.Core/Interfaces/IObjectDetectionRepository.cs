using PhotoAI.Core.Models;

namespace PhotoAI.Core.Interfaces;

public interface IObjectDetectionRepository
{
    Task<IReadOnlyList<ObjectDetection>> GetByMediaItemIdAsync(long mediaItemId);
    Task<IReadOnlyList<ObjectDetection>> GetByLabelAsync(string label, int skip, int take);
    Task<IReadOnlyList<string>> GetAllLabelsAsync();
    Task<long> GetCountByLabelAsync(string label);
    Task AddAsync(ObjectDetection detection);
    Task AddRangeAsync(IEnumerable<ObjectDetection> detections);
    Task DeleteByMediaItemIdAsync(long mediaItemId);
}
