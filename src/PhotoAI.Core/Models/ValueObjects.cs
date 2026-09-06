namespace PhotoAI.Core.Models;

public class MediaItemInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime DateModified { get; set; }
    public DateTime? DateTaken { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? CameraMake { get; set; }
    public string? CameraModel { get; set; }
    public string? Lens { get; set; }
    public int? Iso { get; set; }
    public string? Aperture { get; set; }
    public string? ShutterSpeed { get; set; }
    public int? FocalLength { get; set; }
    public int? Orientation { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? ContentHash { get; set; }
    public MediaType MediaType { get; set; }
}

public class SearchResult
{
    public MediaItem MediaItem { get; set; } = null!;
    public float Score { get; set; }
    public string? MatchReason { get; set; }
    public Dictionary<string, float>? ScoreBreakdown { get; set; }
}

public class DuplicateInfo
{
    public long MediaItemId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public float Similarity { get; set; }
    public DuplicateGroupType Type { get; set; }
}

public class IndexProgress
{
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public int FailedFiles { get; set; }
    public int SkippedFiles { get; set; }
    public string CurrentPhase { get; set; } = string.Empty;
    public string? CurrentFile { get; set; }
    public double FilesPerSecond { get; set; }
    public TimeSpan EstimatedTimeRemaining { get; set; }
}

public class ModelInfo
{
    public string ModelId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string? Path { get; set; }
    public string License { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public bool SupportsCpu { get; set; }
    public bool SupportsCuda { get; set; }
    public bool IsInstalled { get; set; }
    public bool IsEnabled { get; set; }
    public int? EmbeddingDimension { get; set; }
}
