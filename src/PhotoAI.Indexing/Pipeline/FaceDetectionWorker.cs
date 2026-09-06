using Microsoft.Extensions.Logging;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Indexing.Pipeline;

public class FaceDetectionWorker
{
    private readonly ILogger<FaceDetectionWorker> _logger;
    private readonly IFaceDetector _faceDetector;
    private readonly IFaceEmbeddingModel _faceEmbeddingModel;
    private readonly IFaceRepository _faceRepository;
    private readonly IImageProcessor _imageProcessor;

    public FaceDetectionWorker(
        ILogger<FaceDetectionWorker> logger,
        IFaceDetector faceDetector,
        IFaceEmbeddingModel faceEmbeddingModel,
        IFaceRepository faceRepository,
        IImageProcessor imageProcessor)
    {
        _logger = logger;
        _faceDetector = faceDetector;
        _faceEmbeddingModel = faceEmbeddingModel;
        _faceRepository = faceRepository;
        _imageProcessor = imageProcessor;
    }

    public async Task ProcessAsync(MediaItem mediaItem, CancellationToken cancellationToken = default)
    {
        if (mediaItem.MediaType != MediaType.Image)
        {
            _logger.LogDebug("Skipping face detection for non-image: {FileName}", mediaItem.FileName);
            return;
        }

        try
        {
            _logger.LogDebug("Detecting faces in: {FileName}", mediaItem.FileName);

            var imageData = await _imageProcessor.GetImageBytesAsync(mediaItem.FilePath, cancellationToken);
            var detections = await _faceDetector.DetectAsync(imageData, cancellationToken);

            if (detections.Count == 0)
            {
                _logger.LogDebug("No faces detected in: {FileName}", mediaItem.FileName);
                return;
            }

            var faces = new List<Face>();
            
            foreach (var detection in detections)
            {
                // Crop face region
                var faceImageData = await _imageProcessor.CropAsync(
                    imageData,
                    detection.BoundingBoxX,
                    detection.BoundingBoxY,
                    detection.BoundingBoxWidth,
                    detection.BoundingBoxHeight,
                    cancellationToken);

                // Generate face thumbnail
                var faceThumbnail = await _imageProcessor.GetThumbnailAsync(faceImageData, 128, cancellationToken);

                var face = new Face
                {
                    MediaItemId = mediaItem.Id,
                    BoundingBoxX = detection.BoundingBoxX,
                    BoundingBoxY = detection.BoundingBoxY,
                    BoundingBoxWidth = detection.BoundingBoxWidth,
                    BoundingBoxHeight = detection.BoundingBoxHeight,
                    Confidence = detection.Confidence,
                    DateDetected = DateTime.UtcNow
                };

                // Generate face embedding
                var embedding = await _faceEmbeddingModel.GetEmbeddingAsync(faceImageData, cancellationToken);
                face.Embeddings.Add(new FaceEmbedding
                {
                    ModelName = "SFace",
                    ModelVersion = "1.0",
                    Vector = FloatArrayToBytes(embedding),
                    DateCreated = DateTime.UtcNow
                });

                faces.Add(face);
            }

            await _faceRepository.AddRangeAsync(faces);
            
            _logger.LogDebug("Detected {Count} faces in: {FileName}", faces.Count, mediaItem.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect faces in: {FilePath}", mediaItem.FilePath);
        }
    }

    private static byte[] FloatArrayToBytes(float[] array)
    {
        var bytes = new byte[array.Length * 4];
        Buffer.BlockCopy(array, 0, bytes, 0, bytes.Length);
        return bytes;
    }
}