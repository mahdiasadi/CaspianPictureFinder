using Microsoft.Extensions.Logging;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Indexing.Pipeline;

public class MetadataWorker
{
    private readonly ILogger<MetadataWorker> _logger;
    private readonly IImageProcessor _imageProcessor;
    private readonly MetadataExtractor _metadataExtractor;

    public MetadataWorker(ILogger<MetadataWorker> logger, IImageProcessor imageProcessor)
    {
        _logger = logger;
        _imageProcessor = imageProcessor;
        _metadataExtractor = new MetadataExtractor();
    }

    public async Task<MediaItem> ProcessAsync(MediaItemInfo info, long folderId, CancellationToken cancellationToken = default)
    {
        var mediaItem = new MediaItem
        {
            FilePath = info.FilePath,
            FileName = info.FileName,
            Extension = info.Extension,
            FileSize = info.FileSize,
            MediaType = info.MediaType,
            Status = MediaStatus.Processing,
            DateModified = info.DateModified,
            FolderId = folderId,
            DateIndexed = DateTime.UtcNow
        };

        try
        {
            // Extract metadata
            var fullInfo = _metadataExtractor.ExtractFromFile(info.FilePath);
            mediaItem.Width = fullInfo.Width;
            mediaItem.Height = fullInfo.Height;
            mediaItem.DateTaken = fullInfo.DateTaken;
            mediaItem.CameraMake = fullInfo.CameraMake;
            mediaItem.CameraModel = fullInfo.CameraModel;
            mediaItem.Lens = fullInfo.Lens;
            mediaItem.Iso = fullInfo.Iso;
            mediaItem.Aperture = fullInfo.Aperture;
            mediaItem.ShutterSpeed = fullInfo.ShutterSpeed;
            mediaItem.FocalLength = fullInfo.FocalLength;
            mediaItem.Orientation = fullInfo.Orientation;
            mediaItem.Latitude = fullInfo.Latitude;
            mediaItem.Longitude = fullInfo.Longitude;

            // Compute file path hash for deduplication
            mediaItem.FilePathHash = ComputeFilePathHash(info.FilePath);

            // Compute content hash
            if (info.MediaType == MediaType.Image)
            {
                var imageData = await _imageProcessor.GetImageBytesAsync(info.FilePath, cancellationToken);
                mediaItem.ContentHash = await _imageProcessor.ComputeContentHashAsync(imageData, cancellationToken);

                // Compute perceptual hash
                mediaItem.PerceptualHash = await _imageProcessor.ComputePerceptualHashAsync(imageData, cancellationToken);
            }

            // Set date taken fallback
            mediaItem.DateTaken ??= info.DateModified;

            _logger.LogDebug("Processed metadata for: {FileName} ({Width}x{Height})",
                info.FileName, mediaItem.Width, mediaItem.Height);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process metadata for: {FilePath}", info.FilePath);
            mediaItem.Status = MediaStatus.Failed;
            mediaItem.LastError = ex.Message;
        }

        return mediaItem;
    }

    private static string ComputeFilePathHash(string filePath)
    {
        var normalized = filePath.ToLowerInvariant().Replace('/', '\\');
        var bytes = System.Text.Encoding.UTF8.GetBytes(normalized);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
