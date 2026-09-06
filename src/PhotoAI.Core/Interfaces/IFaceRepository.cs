using PhotoAI.Core.Models;

namespace PhotoAI.Core.Interfaces;

public interface IFaceRepository
{
    Task<Face?> GetByIdAsync(long id);
    Task<IReadOnlyList<Face>> GetByMediaItemIdAsync(long mediaItemId);
    Task<IReadOnlyList<Face>> GetByPersonIdAsync(long personId);
    Task<IReadOnlyList<Face>> GetUnknownFacesAsync(int skip, int take);
    Task<long> GetCountByPersonAsync(long personId);
    Task<long> GetUnknownCountAsync();
    Task AddAsync(Face face);
    Task AddRangeAsync(IEnumerable<Face> faces);
    Task UpdateAsync(Face face);
    Task DeleteAsync(long id);
    Task AssignToPersonAsync(IEnumerable<long> faceIds, long personId);
    Task UnassignFromPersonAsync(long faceId);
    Task ClusterUnknownFacesAsync(double threshold);
}
