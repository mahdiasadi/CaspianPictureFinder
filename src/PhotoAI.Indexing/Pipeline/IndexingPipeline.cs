using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Indexing.Pipeline;

public class IndexingPipeline
{
    private readonly ILogger<IndexingPipeline> _logger;
    private readonly IMediaRepository _mediaRepository;
    private readonly IFolderRepository _folderRepository;
    private readonly IIndexJobRepository _indexJobRepository;
    private readonly FileScanner _fileScanner;
    private readonly MetadataWorker _metadataWorker;
    private readonly ThumbnailWorker _thumbnailWorker;
    private readonly FaceDetectionWorker _faceDetectionWorker;
    private readonly ObjectDetectionWorker _objectDetectionWorker;
    private readonly SceneClassificationWorker _sceneClassificationWorker;
    private readonly VideoFrameEmbeddingWorker _videoFrameEmbeddingWorker;
    private readonly SmartAlbumWorker _smartAlbumWorker;
    private readonly DuplicateFinder _duplicateFinder;

    private CancellationTokenSource? _cts;
    private Task? _pipelineTask;

    public event Action<IndexProgress>? ProgressChanged;
    public event Action? IndexingCompleted;
    public event Action<string>? ErrorOccurred;

    public bool IsRunning => _pipelineTask != null && !_pipelineTask.IsCompleted;

    public IndexingPipeline(
        ILogger<IndexingPipeline> logger,
        IMediaRepository mediaRepository,
        IFolderRepository folderRepository,
        IIndexJobRepository indexJobRepository,
        FileScanner fileScanner,
        MetadataWorker metadataWorker,
        ThumbnailWorker thumbnailWorker,
        FaceDetectionWorker faceDetectionWorker,
        ObjectDetectionWorker objectDetectionWorker,
        SceneClassificationWorker sceneClassificationWorker,
        VideoFrameEmbeddingWorker videoFrameEmbeddingWorker,
        SmartAlbumWorker smartAlbumWorker,
        DuplicateFinder duplicateFinder)
    {
        _logger = logger;
        _mediaRepository = mediaRepository;
        _folderRepository = folderRepository;
        _indexJobRepository = indexJobRepository;
        _fileScanner = fileScanner;
        _metadataWorker = metadataWorker;
        _thumbnailWorker = thumbnailWorker;
        _faceDetectionWorker = faceDetectionWorker;
        _objectDetectionWorker = objectDetectionWorker;
        _sceneClassificationWorker = sceneClassificationWorker;
        _videoFrameEmbeddingWorker = videoFrameEmbeddingWorker;
        _smartAlbumWorker = smartAlbumWorker;
        _duplicateFinder = duplicateFinder;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            _logger.LogWarning("Indexing pipeline is already running");
            return;
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var job = new IndexJob
        {
            Name = $"Index_{DateTime.UtcNow:yyyyMMdd_HHmmss}",
            Status = IndexJobStatus.Running,
            DateStarted = DateTime.UtcNow,
            CurrentPhase = "Scanning"
        };
        await _indexJobRepository.AddAsync(job);

        _pipelineTask = RunPipelineAsync(job, _cts.Token);
    }

    public void Pause()
    {
        _cts?.Cancel();
    }

    public void Resume()
    {
        if (!IsRunning)
        {
            _ = StartAsync();
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
    }

    private async Task RunPipelineAsync(IndexJob job, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var progress = new IndexProgress();

        try
        {
            var folders = await _folderRepository.GetEnabledAsync();
            var discoveredChannel = Channel.CreateBounded<MediaItemInfo>(new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = true
            });

            // Phase 1: Scan all folders
            job.CurrentPhase = "Scanning";
            await _indexJobRepository.UpdateAsync(job);

            var scanTask = Task.Run(async () =>
            {
                foreach (var folder in folders)
                {
                    await _fileScanner.ScanDirectoryAsync(folder.Path, discoveredChannel.Writer, folder.RecursiveScan, cancellationToken);
                }
                discoveredChannel.Writer.Complete();
            }, cancellationToken);

            // Phase 2: Process discovered files
            job.CurrentPhase = "Processing";
            await _indexJobRepository.UpdateAsync(job);

            var processedCount = 0;
            var failedCount = 0;
            var skippedCount = 0;
            var batch = new List<MediaItem>();
            const int batch_size = 100;

            await foreach (var info in discoveredChannel.Reader.ReadAllAsync(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Check if already indexed
                if (await _mediaRepository.ExistsByFilePathHashAsync(ComputeFilePathHash(info.FilePath)))
                {
                    skippedCount++;
                    continue;
                }

                // Find or create folder record
                var folderPath = Path.GetDirectoryName(info.FilePath) ?? string.Empty;
                var folder = await _folderRepository.GetByPathAsync(folderPath);
                if (folder == null)
                {
                    folder = new Folder
                    {
                        Path = folderPath,
                        Name = Path.GetFileName(folderPath),
                        DateAdded = DateTime.UtcNow
                    };
                    await _folderRepository.AddAsync(folder);
                }

                try
                {
                    var mediaItem = await _metadataWorker.ProcessAsync(info, folder.Id, cancellationToken);

                    // Generate thumbnail
                    if (mediaItem.Status != MediaStatus.Failed)
                    {
                        var thumbPath = await _thumbnailWorker.GenerateThumbnailAsync(mediaItem, cancellationToken);
                        mediaItem.ThumbnailPath = thumbPath;
                        mediaItem.Status = MediaStatus.Indexed;
                    }

                    // Detect faces
                    if (mediaItem.MediaType == MediaType.Image && mediaItem.Status == MediaStatus.Indexed)
                    {
                        await _faceDetectionWorker.ProcessAsync(mediaItem, cancellationToken);
                    }

                    // Detect objects
                    if (mediaItem.MediaType == MediaType.Image && mediaItem.Status == MediaStatus.Indexed)
                    {
                        await _objectDetectionWorker.ProcessAsync(mediaItem, cancellationToken);
                    }

                    // Classify scene
                    if (mediaItem.MediaType == MediaType.Image && mediaItem.Status == MediaStatus.Indexed)
                    {
                        await _sceneClassificationWorker.ProcessAsync(mediaItem, cancellationToken);
                    }

                    // Process video frames
                    if (mediaItem.MediaType == MediaType.Video && mediaItem.Status == MediaStatus.Indexed)
                    {
                        await _videoFrameEmbeddingWorker.ProcessAsync(mediaItem, cancellationToken);
                    }

                    batch.Add(mediaItem);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process: {FilePath}", info.FilePath);
                    failedCount++;
                }

                // Batch save
                if (batch.Count >= batch_size)
                {
                    await _mediaRepository.AddRangeAsync(batch);
                    batch.Clear();

                    ReportProgress(progress, job, processedCount, failedCount, skippedCount, stopwatch);
                }
            }

            // Save remaining
            if (batch.Count > 0)
            {
                await _mediaRepository.AddRangeAsync(batch);
            }

            // Phase 3: Duplicate detection
            job.CurrentPhase = "Duplicate Detection";
            await _indexJobRepository.UpdateAsync(job);

            await _duplicateFinder.FindExactDuplicatesAsync(cancellationToken);
            await _duplicateFinder.FindNearDuplicatesAsync(cancellationToken: cancellationToken);

            // Phase 4: Smart Albums
            job.CurrentPhase = "Smart Albums";
            await _indexJobRepository.UpdateAsync(job);

            await _smartAlbumWorker.CreateSmartAlbumsAsync(cancellationToken);

            // Complete
            job.Status = IndexJobStatus.Completed;
            job.ProcessedFiles = processedCount;
            job.FailedFiles = failedCount;
            job.SkippedFiles = skippedCount;
            job.DateCompleted = DateTime.UtcNow;
            job.CurrentPhase = "Completed";
            await _indexJobRepository.UpdateAsync(job);

            stopwatch.Stop();
            _logger.LogInformation(
                "Indexing completed: {Processed} processed, {Failed} failed, {Skipped} skipped in {Elapsed}",
                processedCount, failedCount, skippedCount, stopwatch.Elapsed);

            IndexingCompleted?.Invoke();
        }
        catch (OperationCanceledException)
        {
            job.Status = IndexJobStatus.Cancelled;
            await _indexJobRepository.UpdateAsync(job);
            _logger.LogInformation("Indexing cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Indexing pipeline failed");
            job.Status = IndexJobStatus.Failed;
            job.ErrorLog = ex.Message;
            await _indexJobRepository.UpdateAsync(job);
            ErrorOccurred?.Invoke(ex.Message);
        }
    }

    private void ReportProgress(IndexProgress progress, IndexJob job, int processed, int failed, int skipped, Stopwatch stopwatch)
    {
        var elapsed = stopwatch.Elapsed;
        var rate = elapsed.TotalSeconds > 0 ? processed / elapsed.TotalSeconds : 0;
        var remaining = rate > 0 ? TimeSpan.FromSeconds((job.TotalFiles - processed) / rate) : TimeSpan.Zero;

        progress = new IndexProgress
        {
            TotalFiles = job.TotalFiles,
            ProcessedFiles = processed,
            FailedFiles = failed,
            SkippedFiles = skipped,
            CurrentPhase = job.CurrentPhase ?? "Processing",
            FilesPerSecond = rate,
            EstimatedTimeRemaining = remaining
        };

        ProgressChanged?.Invoke(progress);
    }

    private static string ComputeFilePathHash(string filePath)
    {
        var normalized = filePath.ToLowerInvariant().Replace('/', '\\');
        var bytes = System.Text.Encoding.UTF8.GetBytes(normalized);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
