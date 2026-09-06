using Microsoft.Extensions.Logging;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Indexing.Pipeline;

public class OcrWorker
{
    private readonly ILogger<OcrWorker> _logger;
    private readonly IOcrEngine _ocrEngine;
    private readonly IOcrRepository _ocrRepository;
    private readonly IImageProcessor _imageProcessor;

    public OcrWorker(
        ILogger<OcrWorker> logger,
        IOcrEngine ocrEngine,
        IOcrRepository ocrRepository,
        IImageProcessor imageProcessor)
    {
        _logger = logger;
        _ocrEngine = ocrEngine;
        _ocrRepository = ocrRepository;
        _imageProcessor = imageProcessor;
    }

    public async Task ProcessAsync(MediaItem mediaItem, CancellationToken cancellationToken = default)
    {
        if (mediaItem.MediaType != MediaType.Image)
            return;

        try
        {
            _logger.LogDebug("Running OCR on: {FileName}", mediaItem.FileName);

            var imageData = await _imageProcessor.GetImageBytesAsync(mediaItem.FilePath, cancellationToken);
            var ocrResults = await _ocrEngine.RecognizeAsync(imageData, cancellationToken);

            if (ocrResults.Count == 0)
            {
                _logger.LogDebug("No text found in: {FileName}", mediaItem.FileName);
                return;
            }

            var ocrEntities = ocrResults.Select(r => new OcrResult
            {
                MediaItemId = mediaItem.Id,
                Text = r.Text,
                Confidence = r.Confidence,
                BoundingBoxX = r.BoundingBoxX,
                BoundingBoxY = r.BoundingBoxY,
                BoundingBoxWidth = r.BoundingBoxWidth,
                BoundingBoxHeight = r.BoundingBoxHeight,
                Language = r.Language,
                ModelName = "PaddleOCR PP-OCRv5",
                DateExtracted = DateTime.UtcNow
            }).ToList();

            await _ocrRepository.AddRangeAsync(ocrEntities);
            
            _logger.LogDebug("Extracted {Count} text regions from: {FileName}", ocrEntities.Count, mediaItem.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to run OCR on: {FilePath}", mediaItem.FilePath);
        }
    }
}