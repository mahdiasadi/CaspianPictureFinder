using Microsoft.EntityFrameworkCore;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Storage.Repositories;

public class FaceRepository : IFaceRepository
{
    private readonly Func<PhotoAiDbContext> _contextFactory;

    public FaceRepository(Func<PhotoAiDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Face?> GetByIdAsync(long id)
    {
        using var context = _contextFactory();
        return await context.Faces.FindAsync(id);
    }

    public async Task<IReadOnlyList<Face>> GetByMediaItemIdAsync(long mediaItemId)
    {
        using var context = _contextFactory();
        return await context.Faces.Where(f => f.MediaItemId == mediaItemId).ToListAsync();
    }

    public async Task<IReadOnlyList<Face>> GetByPersonIdAsync(long personId)
    {
        using var context = _contextFactory();
        return await context.Faces.Where(f => f.PersonId == personId).ToListAsync();
    }

    public async Task<IReadOnlyList<Face>> GetUnknownFacesAsync(int skip, int take)
    {
        using var context = _contextFactory();
        return await context.Faces
            .Where(f => f.PersonId == null)
            .OrderByDescending(f => f.DateDetected)
            .Skip(skip).Take(take)
            .ToListAsync();
    }

    public async Task<long> GetCountByPersonAsync(long personId)
    {
        using var context = _contextFactory();
        return await context.Faces.CountAsync(f => f.PersonId == personId);
    }

    public async Task<long> GetUnknownCountAsync()
    {
        using var context = _contextFactory();
        return await context.Faces.CountAsync(f => f.PersonId == null);
    }

    public async Task AddAsync(Face face)
    {
        using var context = _contextFactory();
        await context.Faces.AddAsync(face);
        await context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Face> faces)
    {
        using var context = _contextFactory();
        await context.Faces.AddRangeAsync(faces);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Face face)
    {
        using var context = _contextFactory();
        context.Faces.Update(face);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        using var context = _contextFactory();
        var face = await context.Faces.FindAsync(id);
        if (face != null)
        {
            context.Faces.Remove(face);
            await context.SaveChangesAsync();
        }
    }

    public async Task AssignToPersonAsync(IEnumerable<long> faceIds, long personId)
    {
        using var context = _contextFactory();
        var faces = await context.Faces.Where(f => faceIds.Contains(f.Id)).ToListAsync();
        foreach (var face in faces)
        {
            face.PersonId = personId;
        }
        await context.SaveChangesAsync();
    }

    public async Task UnassignFromPersonAsync(long faceId)
    {
        using var context = _contextFactory();
        var face = await context.Faces.FindAsync(faceId);
        if (face != null)
        {
            face.PersonId = null;
            await context.SaveChangesAsync();
        }
    }

    public async Task ClusterUnknownFacesAsync(double threshold)
    {
        using var context = _contextFactory();
        
        var unknownFaces = await context.Faces
            .Where(f => f.PersonId == null)
            .Include(f => f.Embeddings)
            .ToListAsync();

        if (unknownFaces.Count < 2) return;

        var processed = new HashSet<long>();
        int clusterId = 0;

        foreach (var face in unknownFaces)
        {
            if (processed.Contains(face.Id)) continue;
            
            var faceEmbedding = face.Embeddings.FirstOrDefault()?.Vector;
            if (faceEmbedding == null || faceEmbedding.Length == 0) continue;

            var cluster = new List<long> { face.Id };
            processed.Add(face.Id);

            foreach (var otherFace in unknownFaces)
            {
                if (processed.Contains(otherFace.Id)) continue;
                
                var otherEmbedding = otherFace.Embeddings.FirstOrDefault()?.Vector;
                if (otherEmbedding == null || otherEmbedding.Length == 0) continue;

                var similarity = CosineSimilarity(faceEmbedding, otherEmbedding);
                if (similarity >= threshold)
                {
                    cluster.Add(otherFace.Id);
                    processed.Add(otherFace.Id);
                }
            }

            if (cluster.Count > 1)
            {
                clusterId++;
                var person = new Person
                {
                    Name = $"Person {clusterId}",
                    DateCreated = DateTime.UtcNow
                };
                context.Persons.Add(person);
                await context.SaveChangesAsync();

                foreach (var faceId in cluster)
                {
                    var faceEntity = await context.Faces.FindAsync(faceId);
                    if (faceEntity != null)
                        faceEntity.PersonId = person.Id;
                }
                await context.SaveChangesAsync();
            }
        }
    }

    private static double CosineSimilarity(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return 0;
        
        float dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i += 4)
        {
            float va = BitConverter.ToSingle(a, i);
            float vb = BitConverter.ToSingle(b, i);
            dot += va * vb;
            normA += va * va;
            normB += vb * vb;
        }
        
        return normA > 0 && normB > 0 ? dot / (MathF.Sqrt(normA) * MathF.Sqrt(normB)) : 0;
    }
}
