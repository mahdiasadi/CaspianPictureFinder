using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Search;

public class SearchRanker
{
    private readonly IEmbeddingRepository _embeddingRepository;
    private readonly IObjectDetectionRepository _objectRepository;
    private readonly ISceneRepository _sceneRepository;
    private readonly IFaceRepository _faceRepository;
    private readonly IMediaRepository _mediaRepository;
    private readonly IOcrRepository _ocrRepository;

    // Configurable weights (can be loaded from settings)
    private RankingWeights _weights;

    public SearchRanker(
        IEmbeddingRepository embeddingRepository,
        IObjectDetectionRepository objectRepository,
        ISceneRepository sceneRepository,
        IFaceRepository faceRepository,
        IMediaRepository mediaRepository,
        IOcrRepository ocrRepository)
    {
        _embeddingRepository = embeddingRepository;
        _objectRepository = objectRepository;
        _sceneRepository = sceneRepository;
        _faceRepository = faceRepository;
        _mediaRepository = mediaRepository;
        _ocrRepository = ocrRepository;
        
        _weights = new RankingWeights();
    }

    public void UpdateWeights(RankingWeights weights)
    {
        _weights = weights ?? new RankingWeights();
    }

    public RankingWeights GetWeights() => _weights;

    public async Task<IReadOnlyList<SearchResult>> RankResultsAsync(
        IReadOnlyList<(long MediaItemId, float SemanticScore)> semanticResults,
        SearchQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        if (!semanticResults.Any())
            return Array.Empty<SearchResult>();

        var mediaItemIds = semanticResults.Select(s => s.MediaItemId).ToHashSet();
        
        // Batch load all required data to minimize DB round trips
        var mediaItems = await _mediaRepository.GetByIdsAsync(mediaItemIds);
        var mediaItemDict = mediaItems.ToDictionary(m => m.Id);

        var allObjectDetections = new Dictionary<long, List<ObjectDetection>>();
        var allSceneLabels = new Dictionary<long, List<SceneLabel>>();
        var allFaces = new Dictionary<long, List<Face>>();
        var allOcrResults = new Dictionary<long, List<OcrResult>>();

        foreach (var id in mediaItemIds)
        {
            allObjectDetections[id] = (await _objectRepository.GetByMediaItemIdAsync(id)).ToList();
            allSceneLabels[id] = (await _sceneRepository.GetByMediaItemIdAsync(id)).ToList();
            allFaces[id] = (await _faceRepository.GetByMediaItemIdAsync(id)).ToList();
            allOcrResults[id] = (await _ocrRepository.GetByMediaItemIdAsync(id)).ToList();
        }

        var results = new List<SearchResult>();

        foreach (var (mediaItemId, semanticScore) in semanticResults)
        {
            if (!mediaItemDict.TryGetValue(mediaItemId, out var mediaItem))
                continue;

            var scoreBreakdown = new Dictionary<string, float>
            {
                ["semantic"] = semanticScore
            };

            // Calculate individual scores
            var objectScore = CalculateObjectScore(mediaItemId, query, allObjectDetections);
            var sceneScore = CalculateSceneScore(mediaItemId, query, allSceneLabels);
            var faceScore = CalculateFaceScore(mediaItemId, query, allFaces);
            var ocrScore = CalculateOcrScore(mediaItemId, query, allOcrResults);
            var metadataScore = CalculateMetadataScore(mediaItem, query);
            var freshnessScore = CalculateFreshnessScore(mediaItem);
            var qualityScore = await CalculateQualityScoreAsync(mediaItemId);
            var popularityScore = CalculatePopularityScore(mediaItem);

            scoreBreakdown["object"] = objectScore;
            scoreBreakdown["scene"] = sceneScore;
            scoreBreakdown["face"] = faceScore;
            scoreBreakdown["ocr"] = ocrScore;
            scoreBreakdown["metadata"] = metadataScore;
            scoreBreakdown["freshness"] = freshnessScore;
            scoreBreakdown["quality"] = qualityScore;
            scoreBreakdown["popularity"] = popularityScore;

            // Normalize semantic score to [0,1] range
            var normalizedSemantic = NormalizeScore(semanticScore, 0.5f, 0.95f);
            
            // Calculate weighted score with sigmoid normalization for extreme values
            var finalScore = CalculateWeightedScore(
                normalizedSemantic, objectScore, sceneScore, faceScore, ocrScore,
                metadataScore, freshnessScore, qualityScore, popularityScore);

            results.Add(new SearchResult
            {
                MediaItem = mediaItem,
                Score = finalScore,
                ScoreBreakdown = scoreBreakdown
            });
        }

        // Apply reciprocal rank fusion for robustness
        var fusedResults = ApplyReciprocalRankFusion(results, semanticResults);
        
        return fusedResults.OrderByDescending(r => r.Score).ToList();
    }

    private float CalculateObjectScore(long mediaItemId, SearchQuery? query, Dictionary<long, List<ObjectDetection>> allDetections)
    {
        if (query?.Objects?.Count == 0 || !allDetections.TryGetValue(mediaItemId, out var detections))
            return 0f;

        var detectedLabels = detections.Select(o => o.Label.ToLowerInvariant()).ToHashSet();
        var matched = query.Objects.Count(o => detectedLabels.Contains(o.ToLowerInvariant()));
        
        // Weight by confidence
        float weightedSum = 0;
        int matchedCount = 0;
        
        foreach (var obj in query.Objects)
        {
            var match = detections.FirstOrDefault(d => d.Label.Equals(obj, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                weightedSum += match.Confidence;
                matchedCount++;
            }
        }
        
        return matchedCount > 0 ? weightedSum / matchedCount : 0f;
    }

    private float CalculateSceneScore(long mediaItemId, SearchQuery? query, Dictionary<long, List<SceneLabel>> allLabels)
    {
        if (query?.Scenes?.Count == 0 || !allLabels.TryGetValue(mediaItemId, out var labels))
            return 0f;

        var detectedLabels = labels.Select(s => s.Label.ToLowerInvariant()).ToHashSet();
        var matched = query.Scenes.Count(s => detectedLabels.Contains(s.ToLowerInvariant()));
        
        float weightedSum = 0;
        int matchedCount = 0;
        
        foreach (var scene in query.Scenes)
        {
            var match = labels.FirstOrDefault(s => s.Label.Equals(scene, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                weightedSum += match.Confidence;
                matchedCount++;
            }
        }
        
        return matchedCount > 0 ? weightedSum / matchedCount : 0f;
    }

    private float CalculateFaceScore(long mediaItemId, SearchQuery? query, Dictionary<long, List<Face>> allFaces)
    {
        if (query?.PersonIds?.Count == 0 || !allFaces.TryGetValue(mediaItemId, out var faces))
            return 0f;

        var personIds = faces.Where(f => f.PersonId.HasValue).Select(f => f.PersonId!.Value).ToHashSet();
        var matched = query.PersonIds.Count(pid => personIds.Contains(pid));
        
        return matched > 0 ? (float)matched / query.PersonIds.Count : 0f;
    }

    private float CalculateOcrScore(long mediaItemId, SearchQuery? query, Dictionary<long, List<OcrResult>> allOcr)
    {
        if (string.IsNullOrWhiteSpace(query?.Text) || !allOcr.TryGetValue(mediaItemId, out var ocrResults))
            return 0f;

        var queryTerms = query.Text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (!queryTerms.Any()) return 0f;

        float maxScore = 0f;
        
        foreach (var ocr in ocrResults)
        {
            var text = ocr.Text.ToLowerInvariant();
            int matches = queryTerms.Count(t => text.Contains(t));
            if (matches > 0)
            {
                float score = (float)matches / queryTerms.Length * ocr.Confidence;
                maxScore = Math.Max(maxScore, score);
            }
        }
        
        return maxScore;
    }

    private float CalculateMetadataScore(MediaItem mediaItem, SearchQuery? query)
    {
        if (query == null) return 0.5f; // Neutral

        int matches = 0;
        int total = 0;

        // Date range
        if (query.MinDate.HasValue || query.MaxDate.HasValue)
        {
            total++;
            var date = mediaItem.DateTaken ?? mediaItem.DateModified;
            if (date >= (query.MinDate ?? DateTime.MinValue) && date <= (query.MaxDate ?? DateTime.MaxValue))
                matches++;
        }

        // Camera make
        if (!string.IsNullOrEmpty(query.CameraMake))
        {
            total++;
            if (mediaItem.CameraMake?.Contains(query.CameraMake, StringComparison.OrdinalIgnoreCase) == true)
                matches++;
        }

        // Camera model
        if (!string.IsNullOrEmpty(query.CameraModel))
        {
            total++;
            if (mediaItem.CameraModel?.Contains(query.CameraModel, StringComparison.OrdinalIgnoreCase) == true)
                matches++;
        }

        // Extension
        if (!string.IsNullOrEmpty(query.Extension))
        {
            total++;
            if (string.Equals(mediaItem.Extension, query.Extension, StringComparison.OrdinalIgnoreCase))
                matches++;
        }

        // Resolution
        if (query.MinWidth.HasValue || query.MaxWidth.HasValue)
        {
            total++;
            var width = mediaItem.Width ?? 0;
            if (width >= (query.MinWidth ?? 0) && width <= (query.MaxWidth ?? int.MaxValue))
                matches++;
        }

        // Folder
        if (query.FolderId.HasValue)
        {
            total++;
            if (mediaItem.FolderId == query.FolderId.Value)
                matches++;
        }

        // GPS bounds
        if (query.MinLatitude.HasValue && query.MaxLatitude.HasValue && 
            query.MinLongitude.HasValue && query.MaxLongitude.HasValue &&
            mediaItem.Latitude.HasValue && mediaItem.Longitude.HasValue)
        {
            total++;
            if (mediaItem.Latitude >= query.MinLatitude && mediaItem.Latitude <= query.MaxLatitude &&
                mediaItem.Longitude >= query.MinLongitude && mediaItem.Longitude <= query.MaxLongitude)
                matches++;
        }

        // Media types
        if (query.MediaTypes?.Count > 0)
        {
            total++;
            if (query.MediaTypes.Contains(mediaItem.MediaType))
                matches++;
        }

        return total > 0 ? (float)matches / total : 0.5f;
    }

    private float CalculateFreshnessScore(MediaItem mediaItem)
    {
        var date = mediaItem.DateTaken ?? mediaItem.DateModified;
        
        if (date == default(DateTime))
            return 0.5f;

        var ageDays = (DateTime.UtcNow - date).TotalDays;
        
        // Exponential decay: newer = higher score
        // Half-life of 365 days
        return (float)Math.Exp(-ageDays / 365.0 * Math.Log(2));
    }

    private async Task<float> CalculateQualityScoreAsync(long mediaItemId)
    {
        // Placeholder - would use blur detection, exposure analysis, etc.
        // For now return neutral
        return 0.5f;
    }

    private float CalculatePopularityScore(MediaItem mediaItem)
    {
        // Placeholder - would use view count, user ratings, etc.
        // For now return neutral
        return 0.5f;
    }

    private float NormalizeScore(float score, float min, float max)
    {
        if (score <= min) return 0f;
        if (score >= max) return 1f;
        return (score - min) / (max - min);
    }

    private float CalculateWeightedScore(
        float semantic, float objectScore, float scene, float face, float ocr,
        float metadata, float freshness, float quality, float popularity)
    {
        var weights = _weights;
        
        // Apply sigmoid to prevent extreme scores
        float Sigmoid(float x) => 1f / (1f + MathF.Exp(-10f * (x - 0.5f)));
        
        var score = 
            weights.Semantic * Sigmoid(semantic) +
            weights.Object * Sigmoid(objectScore) +
            weights.Scene * Sigmoid(scene) +
            weights.Face * Sigmoid(face) +
            weights.Ocr * Sigmoid(ocr) +
            weights.Metadata * Sigmoid(metadata) +
            weights.Freshness * Sigmoid(freshness) +
            weights.Quality * Sigmoid(quality) +
            weights.Popularity * Sigmoid(popularity);

        var totalWeight = weights.Semantic + weights.Object + weights.Scene + weights.Face + 
                         weights.Ocr + weights.Metadata + weights.Freshness + 
                         weights.Quality + weights.Popularity;
        
        return score / totalWeight;
    }

    private List<SearchResult> ApplyReciprocalRankFusion(
        List<SearchResult> results, 
        IReadOnlyList<(long MediaItemId, float SemanticScore)> semanticResults)
    {
        // Create ranking maps
        var semanticRank = semanticResults
            .Select((r, i) => (MediaItemId: r.MediaItemId, Rank: i + 1))
            .ToDictionary(x => x.MediaItemId, x => x.Rank);

        var fusedRank = results
            .OrderByDescending(r => r.Score)
            .Select((r, i) => (MediaItemId: r.MediaItem.Id, Rank: i + 1))
            .ToDictionary(x => x.MediaItemId, x => x.Rank);

        const int k = 60; // RRF constant

        foreach (var result in results)
        {
            var semanticR = semanticRank.GetValueOrDefault(result.MediaItem.Id, int.MaxValue);
            var fusedR = fusedRank.GetValueOrDefault(result.MediaItem.Id, int.MaxValue);
            
            float rrfScore = 0;
            if (semanticR != int.MaxValue)
                rrfScore += 1f / (k + semanticR);
            if (fusedR != int.MaxValue)
                rrfScore += 1f / (k + fusedR);
            
            // Blend original score with RRF
            result.Score = 0.7f * result.Score + 0.3f * rrfScore;
        }

        return results;
    }
}

public class SearchQuery
{
    public string? Text { get; set; }
    public float[]? TextEmbedding { get; set; }
    public IReadOnlyList<string>? Objects { get; set; }
    public IReadOnlyList<string>? Scenes { get; set; }
    public IReadOnlyList<long>? PersonIds { get; set; }
    public DateTime? MinDate { get; set; }
    public DateTime? MaxDate { get; set; }
    public string? CameraMake { get; set; }
    public string? CameraModel { get; set; }
    public string? Extension { get; set; }
    public double? MinLatitude { get; set; }
    public double? MaxLatitude { get; set; }
    public double? MinLongitude { get; set; }
    public double? MaxLongitude { get; set; }
    public int? MinWidth { get; set; }
    public int? MaxWidth { get; set; }
    public long? FolderId { get; set; }
    public IReadOnlyList<MediaType>? MediaTypes { get; set; }
    public byte[]? ReferenceImageData { get; set; }
}

public class RankingWeights
{
    public float Semantic { get; set; } = 0.35f;
    public float Object { get; set; } = 0.15f;
    public float Scene { get; set; } = 0.10f;
    public float Face { get; set; } = 0.15f;
    public float Ocr { get; set; } = 0.05f;
    public float Metadata { get; set; } = 0.05f;
    public float Freshness { get; set; } = 0.05f;
    public float Quality { get; set; } = 0.05f;
    public float Popularity { get; set; } = 0.05f;

    public RankingWeights() { }

    public float Total => Semantic + Object + Scene + Face + Ocr + Metadata + Freshness + Quality + Popularity;
}