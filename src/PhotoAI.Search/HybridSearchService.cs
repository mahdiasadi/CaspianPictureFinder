using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Search;

public class HybridSearchService
{
    private readonly TextSearch _textSearch;
    private readonly SimilarImageSearch _similarSearch;
    private readonly SearchRanker _ranker;
    private readonly IMediaRepository _mediaRepository;
    private readonly IObjectDetectionRepository _objectRepository;
    private readonly ISceneRepository _sceneRepository;
    private readonly IFaceRepository _faceRepository;
    private readonly IPersonRepository _personRepository;

    public HybridSearchService(
        TextSearch textSearch,
        SimilarImageSearch similarSearch,
        SearchRanker ranker,
        IMediaRepository mediaRepository,
        IObjectDetectionRepository objectRepository,
        ISceneRepository sceneRepository,
        IFaceRepository faceRepository,
        IPersonRepository personRepository)
    {
        _textSearch = textSearch;
        _similarSearch = similarSearch;
        _ranker = ranker;
        _mediaRepository = mediaRepository;
        _objectRepository = objectRepository;
        _sceneRepository = sceneRepository;
        _faceRepository = faceRepository;
        _personRepository = personRepository;
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        int topK = 50,
        CancellationToken cancellationToken = default)
    {
        var allResults = new Dictionary<long, SearchResult>();

        // 1. Semantic text search
        if (!string.IsNullOrWhiteSpace(query.Text))
        {
            var textResults = await _textSearch.SearchAsync(query.Text, topK * 2, cancellationToken: cancellationToken);
            foreach (var r in textResults)
            {
                allResults[r.MediaItem.Id] = r;
            }
        }

        // 2. Similar image search
        if (query.ReferenceImageData != null && query.ReferenceImageData.Length > 0)
        {
            var imageResults = await _similarSearch.SearchByImageAsync(query.ReferenceImageData, topK * 2, cancellationToken: cancellationToken);
            foreach (var r in imageResults)
            {
                if (allResults.TryGetValue(r.MediaItem.Id, out var existing))
                {
                    // Combine scores
                    existing.Score = Math.Max(existing.Score, r.Score);
                    existing.ScoreBreakdown["visual"] = r.Score;
                }
                else
                {
                    allResults[r.MediaItem.Id] = r;
                }
            }
        }

        // 3. Person search
        if (query.PersonIds?.Count > 0)
        {
            foreach (var personId in query.PersonIds)
            {
                var faces = await _faceRepository.GetByPersonIdAsync(personId);
                foreach (var face in faces)
                {
                    if (allResults.TryGetValue(face.MediaItemId, out var existing))
                    {
                        existing.ScoreBreakdown["face"] = 1.0f;
                    }
                    else
                    {
                        var media = await _mediaRepository.GetByIdAsync(face.MediaItemId);
                        if (media != null)
                        {
                            allResults[face.MediaItemId] = new SearchResult
                            {
                                MediaItem = media,
                                Score = 0.5f,
                                ScoreBreakdown = new Dictionary<string, float> { ["face"] = 1.0f }
                            };
                        }
                    }
                }
            }
        }

        // 4. Object filter
        if (query.Objects?.Count > 0)
        {
            foreach (var obj in query.Objects)
            {
                var detections = await _objectRepository.GetByLabelAsync(obj, 0, topK * 2);
                foreach (var det in detections)
                {
                    if (allResults.TryGetValue(det.MediaItemId, out var existing))
                    {
                        existing.ScoreBreakdown["object"] = det.Confidence;
                    }
                    else
                    {
                        var media = await _mediaRepository.GetByIdAsync(det.MediaItemId);
                        if (media != null)
                        {
                            allResults[det.MediaItemId] = new SearchResult
                            {
                                MediaItem = media,
                                Score = det.Confidence * 0.5f,
                                ScoreBreakdown = new Dictionary<string, float> { ["object"] = det.Confidence }
                            };
                        }
                    }
                }
            }
        }

        // 5. Scene filter
        if (query.Scenes?.Count > 0)
        {
            foreach (var scene in query.Scenes)
            {
                var labels = await _sceneRepository.GetByLabelAsync(scene, 0, topK * 2);
                foreach (var lbl in labels)
                {
                    if (allResults.TryGetValue(lbl.MediaItemId, out var existing))
                    {
                        existing.ScoreBreakdown["scene"] = lbl.Confidence;
                    }
                    else
                    {
                        var media = await _mediaRepository.GetByIdAsync(lbl.MediaItemId);
                        if (media != null)
                        {
                            allResults[lbl.MediaItemId] = new SearchResult
                            {
                                MediaItem = media,
                                Score = lbl.Confidence * 0.5f,
                                ScoreBreakdown = new Dictionary<string, float> { ["scene"] = lbl.Confidence }
                            };
                        }
                    }
                }
            }
        }

        // Apply metadata filters
        var filtered = ApplyMetadataFilters(allResults.Values, query);

        // Re-rank using SearchRanker
        var semanticResults = filtered
            .Select(r => (r.MediaItem.Id, r.ScoreBreakdown.GetValueOrDefault("semantic", r.Score)))
            .ToList();

        var rankedResults = await _ranker.RankResultsAsync(semanticResults, query, cancellationToken);

        return rankedResults.Take(topK).ToList();
    }

    private IEnumerable<SearchResult> ApplyMetadataFilters(IEnumerable<SearchResult> results, SearchQuery query)
    {
        var filtered = results.AsEnumerable();

        if (query.MinDate.HasValue || query.MaxDate.HasValue)
        {
            filtered = filtered.Where(r =>
            {
                var date = r.MediaItem.DateTaken ?? r.MediaItem.DateModified;
                return date >= (query.MinDate ?? DateTime.MinValue) && date <= (query.MaxDate ?? DateTime.MaxValue);
            });
        }

        if (!string.IsNullOrEmpty(query.CameraMake))
        {
            filtered = filtered.Where(r =>
                r.MediaItem.CameraMake?.Contains(query.CameraMake, StringComparison.OrdinalIgnoreCase) == true);
        }

        if (!string.IsNullOrEmpty(query.CameraModel))
        {
            filtered = filtered.Where(r =>
                r.MediaItem.CameraModel?.Contains(query.CameraModel, StringComparison.OrdinalIgnoreCase) == true);
        }

        if (!string.IsNullOrEmpty(query.Extension))
        {
            filtered = filtered.Where(r =>
                string.Equals(r.MediaItem.Extension, query.Extension, StringComparison.OrdinalIgnoreCase));
        }

        if (query.MinWidth.HasValue)
        {
            filtered = filtered.Where(r => (r.MediaItem.Width ?? 0) >= query.MinWidth.Value);
        }

        if (query.MaxWidth.HasValue)
        {
            filtered = filtered.Where(r => (r.MediaItem.Width ?? int.MaxValue) <= query.MaxWidth.Value);
        }

        if (query.FolderId.HasValue)
        {
            filtered = filtered.Where(r => r.MediaItem.FolderId == query.FolderId.Value);
        }

        if (query.MinLatitude.HasValue && query.MaxLatitude.HasValue &&
            query.MinLongitude.HasValue && query.MaxLongitude.HasValue)
        {
            filtered = filtered.Where(r =>
                r.MediaItem.Latitude.HasValue && r.MediaItem.Longitude.HasValue &&
                r.MediaItem.Latitude >= query.MinLatitude && r.MediaItem.Latitude <= query.MaxLatitude &&
                r.MediaItem.Longitude >= query.MinLongitude && r.MediaItem.Longitude <= query.MaxLongitude);
        }

        if (query.MediaTypes?.Count > 0)
        {
            filtered = filtered.Where(r => query.MediaTypes.Contains(r.MediaItem.MediaType));
        }

        return filtered;
    }
}