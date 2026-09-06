using Microsoft.Extensions.Logging;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Indexing.Pipeline;

public class VideoFrameEmbeddingWorker
{
    private readonly ILogger<VideoFrameEmbeddingWorker> _logger;
    private readonly IVideoProcessor _videoProcessor;
    private readonly IImageEmbeddingModel _embeddingModel;
    private readonly IVectorStore _vectorStore;
    private readonly IMediaRepository _mediaRepository;

    public VideoFrameEmbeddingWorker(
        ILogger<VideoFrameEmbeddingWorker> logger,
        IVideoProcessor videoProcessor,
        IImageEmbeddingModel embeddingModel,
        IVectorStore vectorStore,
        IMediaRepository mediaRepository)
    {
        _logger = logger;
        _videoProcessor = videoProcessor;
        _embeddingModel = embeddingModel;
        _vectorStore = vectorStore;
        _mediaRepository = mediaRepository;
    }

    public async Task ProcessAsync(MediaItem mediaItem, CancellationToken cancellationToken = default)
    {
        if (mediaItem.MediaType != MediaType.Video)
            return;

        try
        {
            _logger.LogDebug("Extracting video frames for: {FileName}", mediaItem.FileName);

            // Extract keyframes
            var frames = await _videoProcessor.ExtractKeyframesAsync(
                mediaItem.FilePath, 
                maxFrames: 20, 
                cancellationToken);

            if (frames.Count == 0)
            {
                _logger.LogDebug("No frames extracted from: {FileName}", mediaItem.FileName);
                return;
            }

            // Store video frames metadata
            var videoFrames = new List<VideoFrame>();
            
            foreach (var (timestamp, frameData) in frames)
            {
                var videoFrame = new VideoFrame
                {
                    MediaItemId = mediaItem.Id,
                    TimestampSeconds = timestamp,
                    DateExtracted = DateTime.UtcNow,
                    IsKeyframe = true
                };

                // Generate embedding for frame
                var embedding = await _embeddingModel.GetEmbeddingAsync(frameData, cancellationToken);
                videoFrame.Embeddings.Add(new Embedding
                {
                    ModelName = "SigLIP2",
                    ModelVersion = "1.0",
                    EmbeddingType = "video_frame",
                    Vector = FloatArrayToBytes(embedding),
                    Dimension = embedding.Length,
                    DateCreated = DateTime.UtcNow
                });

                videoFrames.Add(videoFrame);
            }

            // Store frames in database
            // Note: In production, use repository to save VideoFrames
            
            // Index frame embeddings in vector store
            foreach (var frame in videoFrames)
            {
                var embedding = frame.Embeddings.FirstOrDefault();
                if (embedding != null)
                {
                    var vector = BytesToFloatArray(embedding.Vector);
                    await _vectorStore.UpsertAsync("video_frames", frame.Id, vector, cancellationToken);
                }
            }

            _logger.LogDebug("Processed {Count} video frames for: {FileName}", videoFrames.Count, mediaItem.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process video frames for: {FilePath}", mediaItem.FilePath);
        }
    }

    private static byte[] FloatArrayToBytes(float[] array)
    {
        var bytes = new byte[array.Length * 4];
        Buffer.BlockCopy(array, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private static float[] BytesToFloatArray(byte[] bytes)
    {
        var array = new float[bytes.Length / 4];
        Buffer.BlockCopy(bytes, 0, array, 0, bytes.Length);
        return array;
    }
}