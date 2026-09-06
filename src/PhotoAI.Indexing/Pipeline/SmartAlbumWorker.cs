using Microsoft.Extensions.Logging;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Indexing.Pipeline;

public class SmartAlbumWorker
{
    private readonly ILogger<SmartAlbumWorker> _logger;
    private readonly IAlbumRepository _albumRepository;
    private readonly IMediaRepository _mediaRepository;
    private readonly IObjectDetectionRepository _objectRepository;
    private readonly ISceneRepository _sceneRepository;
    private readonly IFaceRepository _faceRepository;
    private readonly IPersonRepository _personRepository;

    public SmartAlbumWorker(
        ILogger<SmartAlbumWorker> logger,
        IAlbumRepository albumRepository,
        IMediaRepository mediaRepository,
        IObjectDetectionRepository objectRepository,
        ISceneRepository sceneRepository,
        IFaceRepository faceRepository,
        IPersonRepository personRepository)
    {
        _logger = logger;
        _albumRepository = albumRepository;
        _mediaRepository = mediaRepository;
        _objectRepository = objectRepository;
        _sceneRepository = sceneRepository;
        _faceRepository = faceRepository;
        _personRepository = personRepository;
    }

    public async Task CreateSmartAlbumsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating smart albums...");

        await CreateAlbumAsync("People", "Photos with detected faces", 
            async ct => await GetMediaWithFacesAsync(), cancellationToken);
        
        await CreateAlbumAsync("Animals", "Photos with animals",
            async ct => await GetMediaWithObjectsAsync(new[] { "dog", "cat", "bird", "horse", "cow", "sheep" }), cancellationToken);

        await CreateAlbumAsync("Vehicles", "Photos with vehicles",
            async ct => await GetMediaWithObjectsAsync(new[] { "car", "truck", "bus", "motorcycle", "bicycle", "boat", "airplane" }), cancellationToken);

        await CreateAlbumAsync("Nature", "Outdoor nature scenes",
            async ct => await GetMediaWithScenesAsync(new[] { "forest", "mountain", "beach", "lake", "river", "waterfall", "sunset", "sky" }), cancellationToken);

        await CreateAlbumAsync("Travel", "Travel and landmark photos",
            async ct => await GetMediaWithScenesAsync(new[] { "city", "building", "street", "bridge", "tower", "monument", "airport", "train_station" }), cancellationToken);

        await CreateAlbumAsync("Food", "Food and dining photos",
            async ct => await GetMediaWithObjectsAsync(new[] { "food", "pizza", "burger", "salad", "fruit", "cake", "coffee", "wine" }), cancellationToken);

        await CreateAlbumAsync("Screenshots", "Screenshots and screen captures",
            async ct => await GetMediaWithObjectsAsync(new[] { "screenshot", "screen", "monitor", "display" }), cancellationToken);

        await CreateAlbumAsync("Documents", "Documents and receipts",
            async ct => await GetMediaWithObjectsAsync(new[] { "document", "receipt", "paper", "text", "form", "table" }), cancellationToken);

        await CreateAlbumAsync("Portraits", "Portrait photos",
            async ct => await GetMediaWithCriteriaAsync(m => m.MediaType == MediaType.Image && HasFaces(m.Id)), cancellationToken);

        await CreateAlbumAsync("Selfies", "Selfie photos",
            async ct => await GetMediaWithCriteriaAsync(m => m.MediaType == MediaType.Image && IsSelfie(m.Id)), cancellationToken);

        await CreateAlbumAsync("Night", "Night and low-light photos",
            async ct => await GetMediaWithCriteriaAsync(m => IsNightPhoto(m.Id)), cancellationToken);

        await CreateAlbumAsync("Favorites", "High-quality favorite photos",
            async ct => await GetMediaWithCriteriaAsync(m => IsFavorite(m.Id)), cancellationToken);

        _logger.LogInformation("Smart albums created/updated");
    }

    private async Task CreateAlbumAsync(
        string name, 
        string description, 
        Func<CancellationToken, Task<List<long>>> getMediaIds, 
        CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _albumRepository.GetAllAsync();
            var album = existing.FirstOrDefault(a => a.Name == name && a.IsSmartAlbum);

            var mediaIds = await getMediaIds(cancellationToken);

            if (album == null)
            {
                album = new Album
                {
                    Name = name,
                    Description = description,
                    IsSmartAlbum = true,
                    DateCreated = DateTime.UtcNow,
                    MediaCount = mediaIds.Count
                };
                await _albumRepository.AddAsync(album);
            }
            else
            {
                album.Description = description;
                album.MediaCount = mediaIds.Count;
                album.DateModified = DateTime.UtcNow;
                await _albumRepository.UpdateAsync(album);
            }

            // Update album-media associations
            await UpdateAlbumMediaAsync(album.Id, mediaIds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create/update smart album: {Name}", name);
        }
    }

    private async Task UpdateAlbumMediaAsync(long albumId, List<long> mediaIds)
    {
        var currentMedia = await _albumRepository.GetMediaByAlbumIdAsync(albumId);
        var currentIds = currentMedia.Select(m => m.Id).ToHashSet();
        var newIds = mediaIds.ToHashSet();

        // Remove media not in new list
        foreach (var id in currentIds.Where(id => !newIds.Contains(id)))
        {
            await _albumRepository.RemoveMediaFromAlbumAsync(albumId, id);
        }

        // Add new media
        foreach (var id in newIds.Where(id => !currentIds.Contains(id)))
        {
            await _albumRepository.AddMediaToAlbumAsync(albumId, id);
        }
    }

    private async Task<List<long>> GetMediaWithFacesAsync()
    {
        var media = await _mediaRepository.GetByStatusAsync(MediaStatus.Indexed, 0, int.MaxValue);
        var result = new List<long>();

        foreach (var m in media.Where(m => m.MediaType == MediaType.Image))
        {
            var faces = await _faceRepository.GetByMediaItemIdAsync(m.Id);
            if (faces.Count > 0)
                result.Add(m.Id);
        }
        return result;
    }

    private async Task<List<long>> GetMediaWithObjectsAsync(string[] labels)
    {
        var result = new List<long>();
        var labelSet = labels.Select(l => l.ToLowerInvariant()).ToHashSet();

        foreach (var label in labels)
        {
            var detections = await _objectRepository.GetByLabelAsync(label, 0, int.MaxValue);
            foreach (var d in detections)
            {
                if (!result.Contains(d.MediaItemId))
                    result.Add(d.MediaItemId);
            }
        }
        return result;
    }

    private async Task<List<long>> GetMediaWithScenesAsync(string[] labels)
    {
        var result = new List<long>();
        var labelSet = labels.Select(l => l.ToLowerInvariant()).ToHashSet();

        foreach (var label in labels)
        {
            var scenes = await _sceneRepository.GetByLabelAsync(label, 0, int.MaxValue);
            foreach (var s in scenes)
            {
                if (!result.Contains(s.MediaItemId))
                    result.Add(s.MediaItemId);
            }
        }
        return result;
    }

    private async Task<List<long>> GetMediaWithCriteriaAsync(Func<MediaItem, bool> criteria)
    {
        var media = await _mediaRepository.GetByStatusAsync(MediaStatus.Indexed, 0, int.MaxValue);
        return media.Where(criteria).Select(m => m.Id).ToList();
    }

    private bool HasFaces(long mediaItemId)
    {
        var faces = _faceRepository.GetByMediaItemIdAsync(mediaItemId).Result;
        return faces.Count > 0;
    }

    private bool IsSelfie(long mediaItemId)
    {
        var faces = _faceRepository.GetByMediaItemIdAsync(mediaItemId).Result;
        return faces.Count == 1 && faces[0].BoundingBoxWidth > 100 && faces[0].BoundingBoxHeight > 100;
    }

    private bool IsNightPhoto(long mediaItemId)
    {
        // Check ISO, exposure, or scene labels for night
        var media = _mediaRepository.GetByIdAsync(mediaItemId).Result;
        if (media?.Iso.HasValue == true && media.Iso.Value > 800) return true;
        
        var scenes = _sceneRepository.GetByMediaItemIdAsync(mediaItemId).Result;
        return scenes.Any(s => s.Label.Contains("night", StringComparison.OrdinalIgnoreCase) || 
                               s.Label.Contains("dark", StringComparison.OrdinalIgnoreCase));
    }

    private bool IsFavorite(long mediaItemId)
    {
        // Placeholder - could be based on user tags, ratings, etc.
        var media = _mediaRepository.GetByIdAsync(mediaItemId).Result;
        return media?.Tags.Any(t => t.Tag == "favorite") == true;
    }
}