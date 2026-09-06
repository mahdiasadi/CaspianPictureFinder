using Microsoft.Extensions.Logging;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Indexing.Pipeline;

public class CaptionWorker
{
    private readonly ILogger<CaptionWorker> _logger;
    private readonly IImageCaptioner _captioner;
    private readonly IMediaRepository _mediaRepository;
    private readonly IImageProcessor _imageProcessor;

    public CaptionWorker(
        ILogger<CaptionWorker> logger,
        IImageCaptioner captioner,
        IMediaRepository mediaRepository,
        IImageProcessor imageProcessor)
    {
        _logger = logger;
        _captioner = captioner;
        _mediaRepository = mediaRepository;
        _imageProcessor = imageProcessor;
    }

    public async Task ProcessAsync(MediaItem mediaItem, CancellationToken cancellationToken = default)
    {
        if (mediaItem.MediaType != MediaType.Image)
            return;

        try
        {
            _logger.LogDebug("Generating caption for: {FileName}", mediaItem.FileName);

            var imageData = await _imageProcessor.GetImageBytesAsync(mediaItem.FilePath, cancellationToken);
            var caption = await _captioner.GenerateCaptionAsync(imageData, cancellationToken);

            if (!string.IsNullOrWhiteSpace(caption))
            {
                mediaItem.Caption = caption;
                await _mediaRepository.UpdateAsync(mediaItem);
                
                _logger.LogDebug("Generated caption for: {FileName}", mediaItem.FileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate caption for: {FilePath}", mediaItem.FilePath);
        }
    }
}