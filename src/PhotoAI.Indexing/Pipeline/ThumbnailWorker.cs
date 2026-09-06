using Microsoft.Extensions.Logging;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Indexing.Pipeline;

public class ThumbnailWorker
{
    private readonly ILogger<ThumbnailWorker> _logger;
    private readonly IImageProcessor _imageProcessor;
    private readonly string _thumbnailDirectory;

    public ThumbnailWorker(ILogger<ThumbnailWorker> logger, IImageProcessor imageProcessor, string thumbnailDirectory)
    {
        _logger = logger;
        _imageProcessor = imageProcessor;
        _thumbnailDirectory = thumbnailDirectory;

        if (!Directory.Exists(_thumbnailDirectory))
        {
            Directory.CreateDirectory(_thumbnailDirectory);
        }
    }

    public async Task<string?> GenerateThumbnailAsync(MediaItem mediaItem, CancellationToken cancellationToken = default)
    {
        if (mediaItem.MediaType != MediaType.Image)
            return null;

        try
        {
            var imageData = await _imageProcessor.GetImageBytesAsync(mediaItem.FilePath, cancellationToken);
            var thumbnailData = await _imageProcessor.GetThumbnailAsync(imageData, 256, cancellationToken);

            var thumbnailFileName = $"{mediaItem.FilePathHash}.jpg";
            var thumbnailPath = Path.Combine(_thumbnailDirectory, thumbnailFileName);

            await File.WriteAllBytesAsync(thumbnailPath, thumbnailData, cancellationToken);

            _logger.LogDebug("Generated thumbnail for: {FileName}", mediaItem.FileName);
            return thumbnailPath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate thumbnail for: {FilePath}", mediaItem.FilePath);
            return null;
        }
    }

    public string GetThumbnailPath(string filePathHash)
    {
        return Path.Combine(_thumbnailDirectory, $"{filePathHash}.jpg");
    }

    public bool ThumbnailExists(string filePathHash)
    {
        return File.Exists(GetThumbnailPath(filePathHash));
    }
}
