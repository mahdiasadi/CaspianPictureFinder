using Microsoft.Extensions.Logging;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Indexing.Pipeline;

public class SceneClassificationWorker
{
    private readonly ILogger<SceneClassificationWorker> _logger;
    private readonly ISceneClassifier _sceneClassifier;
    private readonly ISceneRepository _sceneRepository;
    private readonly IImageProcessor _imageProcessor;

    public SceneClassificationWorker(
        ILogger<SceneClassificationWorker> logger,
        ISceneClassifier sceneClassifier,
        ISceneRepository sceneRepository,
        IImageProcessor imageProcessor)
    {
        _logger = logger;
        _sceneClassifier = sceneClassifier;
        _sceneRepository = sceneRepository;
        _imageProcessor = imageProcessor;
    }

    public async Task ProcessAsync(MediaItem mediaItem, CancellationToken cancellationToken = default)
    {
        if (mediaItem.MediaType != MediaType.Image)
            return;

        try
        {
            _logger.LogDebug("Classifying scene in: {FileName}", mediaItem.FileName);

            var imageData = await _imageProcessor.GetImageBytesAsync(mediaItem.FilePath, cancellationToken);
            var scenes = await _sceneClassifier.ClassifyAsync(imageData, cancellationToken);

            if (scenes.Count == 0)
            {
                _logger.LogDebug("No scene classified in: {FileName}", mediaItem.FileName);
                return;
            }

            var sceneLabels = scenes
                .Where(s => s.Confidence > 0.15f) // Filter low confidence
                .Select(s => new SceneLabel
                {
                    MediaItemId = mediaItem.Id,
                    Label = s.Label,
                    Confidence = s.Confidence,
                    DateDetected = DateTime.UtcNow
                }).ToList();

            if (sceneLabels.Count > 0)
            {
                await _sceneRepository.AddRangeAsync(sceneLabels);
                _logger.LogDebug("Classified {Count} scenes in: {FileName}", sceneLabels.Count, mediaItem.FileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to classify scene in: {FilePath}", mediaItem.FilePath);
        }
    }
}