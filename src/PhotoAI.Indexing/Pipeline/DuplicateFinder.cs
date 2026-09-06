using Microsoft.Extensions.Logging;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Indexing.Pipeline;

public class DuplicateFinder
{
    private readonly ILogger<DuplicateFinder> _logger;
    private readonly IDuplicateRepository _duplicateRepository;
    private readonly IMediaRepository _mediaRepository;

    public DuplicateFinder(
        ILogger<DuplicateFinder> logger,
        IDuplicateRepository duplicateRepository,
        IMediaRepository mediaRepository)
    {
        _logger = logger;
        _duplicateRepository = duplicateRepository;
        _mediaRepository = mediaRepository;
    }

    public async Task FindExactDuplicatesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting exact duplicate detection...");

        // Group by content hash (SHA-256)
        // We need to batch this to avoid loading all media into memory
        const int batchSize = 10000;
        int skip = 0;
        var hashGroups = new Dictionary<string, List<long>>();

        while (true)
        {
            var batch = await _mediaRepository.GetAllAsync(skip, batchSize);
            if (batch.Count == 0) break;

            foreach (var item in batch)
            {
                if (!string.IsNullOrEmpty(item.ContentHash))
                {
                    if (!hashGroups.TryGetValue(item.ContentHash, out var list))
                    {
                        list = new List<long>();
                        hashGroups[item.ContentHash] = list;
                    }
                    list.Add(item.Id);
                }
            }

            skip += batchSize;
            if (batch.Count < batchSize) break;
        }

        int groupCount = 0;
        foreach (var (hash, ids) in hashGroups.Where(g => g.Value.Count > 1))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var group = new DuplicateGroup
            {
                GroupType = DuplicateGroupType.Exact,
                SimilarityScore = 1.0f,
                DateDetected = DateTime.UtcNow
            };

            await _duplicateRepository.AddGroupAsync(group);

            var entries = ids.Select(id => new DuplicateEntry
            {
                DuplicateGroupId = group.Id,
                MediaItemId = id,
                IsSelected = false
            }).ToList();

            await _duplicateRepository.AddEntriesAsync(entries);
            groupCount++;
        }

        _logger.LogInformation("Found {GroupCount} exact duplicate groups", groupCount);
    }

    public async Task FindNearDuplicatesAsync(double similarityThreshold = 0.85, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting near-duplicate detection with threshold {Threshold}...", similarityThreshold);

        const int batchSize = 5000;
        int skip = 0;
        var allHashes = new List<(long Id, ulong Hash)>();

        while (true)
        {
            var batch = await _mediaRepository.GetAllAsync(skip, batchSize);
            if (batch.Count == 0) break;

            foreach (var item in batch)
            {
                if (item.PerceptualHash.HasValue)
                {
                    allHashes.Add((item.Id, item.PerceptualHash.Value));
                }
            }

            skip += batchSize;
            if (batch.Count < batchSize) break;
        }

        _logger.LogInformation("Comparing {Count} perceptual hashes...", allHashes.Count);

        var matched = new HashSet<long>();
        int groupCount = 0;

        for (int i = 0; i < allHashes.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (matched.Contains(allHashes[i].Id)) continue;

            var similarIds = new List<long> { allHashes[i].Id };

            for (int j = i + 1; j < allHashes.Count; j++)
            {
                if (matched.Contains(allHashes[j].Id)) continue;

                double similarity = Media.Processing.ImageProcessor.ComputeSimilarity(allHashes[i].Hash, allHashes[j].Hash);
                if (similarity >= similarityThreshold)
                {
                    similarIds.Add(allHashes[j].Id);
                    matched.Add(allHashes[j].Id);
                }
            }

            if (similarIds.Count > 1)
            {
                var group = new DuplicateGroup
                {
                    GroupType = DuplicateGroupType.NearDuplicate,
                    SimilarityScore = (float)similarityThreshold,
                    DateDetected = DateTime.UtcNow
                };

                await _duplicateRepository.AddGroupAsync(group);

                var entries = similarIds.Select(id => new DuplicateEntry
                {
                    DuplicateGroupId = group.Id,
                    MediaItemId = id,
                    IsSelected = false
                }).ToList();

                await _duplicateRepository.AddEntriesAsync(entries);
                groupCount++;
            }
        }

        _logger.LogInformation("Found {GroupCount} near-duplicate groups", groupCount);
    }
}
