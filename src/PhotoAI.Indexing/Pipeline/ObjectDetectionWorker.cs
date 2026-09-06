using Microsoft.Extensions.Logging;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Indexing.Pipeline;

public class ObjectDetectionWorker
{
    private readonly ILogger<ObjectDetectionWorker> _logger;
    private readonly IObjectDetector _objectDetector;
    private readonly IObjectDetectionRepository _objectRepository;
    private readonly IImageProcessor _imageProcessor;

    public ObjectDetectionWorker(
        ILogger<ObjectDetectionWorker> logger,
        IObjectDetector objectDetector,
        IObjectDetectionRepository objectRepository,
        IImageProcessor imageProcessor)
    {
        _logger = logger;
        _objectDetector = objectDetector;
        _objectRepository = objectRepository;
        _imageProcessor = imageProcessor;
    }

    public async Task ProcessAsync(MediaItem mediaItem, CancellationToken cancellationToken = default)
    {
        if (mediaItem.MediaType != MediaType.Image)
            return;

        try
        {
            _logger.LogDebug("Detecting objects in: {FileName}", mediaItem.FileName);

            var imageData = await _imageProcessor.GetImageBytesAsync(mediaItem.FilePath, cancellationToken);
            var detections = await _objectDetector.DetectAsync(imageData, cancellationToken);

            if (detections.Count == 0)
            {
                _logger.LogDebug("No objects detected in: {FileName}", mediaItem.FileName);
                return;
            }

            var objects = detections.Select(d => new ObjectDetection
            {
                MediaItemId = mediaItem.Id,
                Label = d.Label,
                Confidence = d.Confidence,
                BoundingBoxX = d.BoundingBoxX,
                BoundingBoxY = d.BoundingBoxY,
                BoundingBoxWidth = d.BoundingBoxWidth,
                BoundingBoxHeight = d.BoundingBoxHeight,
                DateDetected = DateTime.UtcNow
            }).ToList();

            await _objectRepository.AddRangeAsync(objects);
            
            _logger.LogDebug("Detected {Count} objects in: {FileName}", objects.Count, mediaItem.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect objects in: {FilePath}", mediaItem.FilePath);
        }
    }
}