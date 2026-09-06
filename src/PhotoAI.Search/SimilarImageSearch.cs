using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Search;

public class SimilarImageSearch
{
    private readonly IVectorStore _vectorStore;
    private readonly IImageEmbeddingModel _embeddingModel;
    private readonly IMediaRepository _mediaRepository;

    public SimilarImageSearch(
        IVectorStore vectorStore,
        IImageEmbeddingModel embeddingModel,
        IMediaRepository mediaRepository)
    {
        _vectorStore = vectorStore;
        _embeddingModel = embeddingModel;
        _mediaRepository = mediaRepository;
    }

    public async Task<IReadOnlyList<SearchResult>> SearchByImageAsync(
        byte[] imageData,
        int topK = 50,
        float similarityThreshold = 0.5f,
        CancellationToken cancellationToken = default)
    {
        // Generate embedding for query image
        var queryEmbedding = await _embeddingModel.GetEmbeddingAsync(imageData, cancellationToken);
        
        // Search vector store
        var results = await _vectorStore.SearchAsync("image_embeddings", queryEmbedding, topK, cancellationToken);
        
        var searchResults = new List<SearchResult>();
        
        foreach (var (mediaItemId, score) in results)
        {
            if (score < similarityThreshold) continue;

            var mediaItem = await _mediaRepository.GetByIdAsync(mediaItemId);
            if (mediaItem == null) continue;

            searchResults.Add(new SearchResult
            {
                MediaItem = mediaItem,
                Score = score,
                MatchReason = $"Visual similarity: {score:P1}",
                ScoreBreakdown = new Dictionary<string, float> { ["visual"] = score }
            });
        }

        return searchResults.OrderByDescending(r => r.Score).ToList();
    }

    public async Task<IReadOnlyList<SearchResult>> SearchByImageFileAsync(
        string imagePath,
        int topK = 50,
        float similarityThreshold = 0.5f,
        CancellationToken cancellationToken = default)
    {
        var imageData = await File.ReadAllBytesAsync(imagePath, cancellationToken);
        return await SearchByImageAsync(imageData, topK, similarityThreshold, cancellationToken);
    }

    public async Task IndexImageAsync(MediaItem mediaItem, CancellationToken cancellationToken = default)
    {
        if (mediaItem.MediaType != MediaType.Image) return;
        
        try
        {
            var imageData = await File.ReadAllBytesAsync(mediaItem.FilePath, cancellationToken);
            var embedding = await _embeddingModel.GetEmbeddingAsync(imageData, cancellationToken);
            
            await _vectorStore.UpsertAsync("image_embeddings", mediaItem.Id, embedding, cancellationToken);
        }
        catch (Exception ex)
        {
            // Log error but don't fail indexing
        }
    }

    public async Task RemoveImageAsync(long mediaItemId, CancellationToken cancellationToken = default)
    {
        await _vectorStore.DeleteAsync("image_embeddings", mediaItemId, cancellationToken);
    }

    public async Task ReindexAllAsync(IEnumerable<MediaItem> mediaItems, CancellationToken cancellationToken = default)
    {
        var batch = new List<(long Id, float[] Vector)>();
        
        foreach (var item in mediaItems)
        {
            if (item.MediaType != MediaType.Image) continue;
            
            try
            {
                var imageData = await File.ReadAllBytesAsync(item.FilePath, cancellationToken);
                var embedding = await _embeddingModel.GetEmbeddingAsync(imageData, cancellationToken);
                batch.Add((item.Id, embedding));
                
                if (batch.Count >= 100)
                {
                    await _vectorStore.UpsertBatchAsync("image_embeddings", batch, cancellationToken);
                    batch.Clear();
                }
            }
            catch { }
        }

        if (batch.Count > 0)
        {
            await _vectorStore.UpsertBatchAsync("image_embeddings", batch, cancellationToken);
        }
    }
}