using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Search;

public class TextSearch
{
    private readonly IVectorStore _vectorStore;
    private readonly ITextEmbeddingModel _embeddingModel;
    private readonly IMediaRepository _mediaRepository;

    public TextSearch(
        IVectorStore vectorStore,
        ITextEmbeddingModel embeddingModel,
        IMediaRepository mediaRepository)
    {
        _vectorStore = vectorStore;
        _embeddingModel = embeddingModel;
        _mediaRepository = mediaRepository;
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query,
        int topK = 50,
        float similarityThreshold = 0.2f,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<SearchResult>();

        // Generate text embedding
        var queryEmbedding = await _embeddingModel.GetEmbeddingAsync(query, cancellationToken);

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
                MatchReason = $"Text match: \"{query}\" ({score:P1})",
                ScoreBreakdown = new Dictionary<string, float> { ["semantic"] = score }
            });
        }

        return searchResults.OrderByDescending(r => r.Score).ToList();
    }
}