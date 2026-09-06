using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Search;

public class VideoSearch
{
    private readonly IVectorStore _vectorStore;
    private readonly IMediaRepository _mediaRepository;

    public VideoSearch(
        IVectorStore vectorStore,
        IMediaRepository mediaRepository)
    {
        _vectorStore = vectorStore;
        _mediaRepository = mediaRepository;
    }

    public async Task<IReadOnlyList<VideoSearchResult>> SearchByTextAsync(
        string query,
        ITextEmbeddingModel textEmbeddingModel,
        int topK = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<VideoSearchResult>();

        // Generate text embedding
        var queryEmbedding = await textEmbeddingModel.GetEmbeddingAsync(query, cancellationToken);

        // Search video frame embeddings
        var results = await _vectorStore.SearchAsync("video_frames", queryEmbedding, topK * 2, cancellationToken);

        var searchResults = new List<VideoSearchResult>();
        var seenMediaIds = new HashSet<long>();

        foreach (var (frameId, score) in results)
        {
            if (score < 0.3f) continue; // Threshold

            var frame = await GetVideoFrameAsync(frameId);
            if (frame == null) continue;

            var mediaItem = await _mediaRepository.GetByIdAsync(frame.MediaItemId);
            if (mediaItem == null || mediaItem.MediaType != MediaType.Video) continue;

            // Deduplicate by media item - keep best timestamp match
            if (seenMediaIds.Contains(mediaItem.Id))
                continue;

            seenMediaIds.Add(mediaItem.Id);

            searchResults.Add(new VideoSearchResult
            {
                MediaItem = mediaItem,
                Score = score,
                TimestampSeconds = frame.TimestampSeconds,
                MatchReason = $"Video content match: {score:P1}"
            });

            if (searchResults.Count >= topK) break;
        }

        return searchResults.OrderByDescending(r => r.Score).ToList();
    }

    public async Task<IReadOnlyList<VideoSearchResult>> SearchByImageAsync(
        byte[] imageData,
        IImageEmbeddingModel imageEmbeddingModel,
        int topK = 20,
        CancellationToken cancellationToken = default)
    {
        var queryEmbedding = await imageEmbeddingModel.GetEmbeddingAsync(imageData, cancellationToken);

        var results = await _vectorStore.SearchAsync("video_frames", queryEmbedding, topK * 2, cancellationToken);

        var searchResults = new List<VideoSearchResult>();
        var seenMediaIds = new HashSet<long>();

        foreach (var (frameId, score) in results)
        {
            if (score < 0.3f) continue;

            var frame = await GetVideoFrameAsync(frameId);
            if (frame == null) continue;

            var mediaItem = await _mediaRepository.GetByIdAsync(frame.MediaItemId);
            if (mediaItem == null || mediaItem.MediaType != MediaType.Video) continue;

            if (seenMediaIds.Contains(mediaItem.Id))
                continue;

            seenMediaIds.Add(mediaItem.Id);

            searchResults.Add(new VideoSearchResult
            {
                MediaItem = mediaItem,
                Score = score,
                TimestampSeconds = frame.TimestampSeconds,
                MatchReason = $"Visual match: {score:P1}"
            });

            if (searchResults.Count >= topK) break;
        }

        return searchResults.OrderByDescending(r => r.Score).ToList();
    }

    private async Task<VideoFrame?> GetVideoFrameAsync(long frameId)
    {
        // In production, use repository to get VideoFrame by ID
        // This is a placeholder
        return null;
    }
}

public class VideoSearchResult : SearchResult
{
    public double TimestampSeconds { get; set; }
}