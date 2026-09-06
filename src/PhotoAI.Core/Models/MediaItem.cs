namespace PhotoAI.Core.Models;

public class MediaItem
{
    public long Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public MediaType MediaType { get; set; }
    public MediaStatus Status { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public DateTime? DateTaken { get; set; }
    public DateTime DateModified { get; set; }
    public DateTime DateIndexed { get; set; }
    public string? ContentHash { get; set; }
    public ulong? PerceptualHash { get; set; }
    public string? FilePathHash { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? CameraMake { get; set; }
    public string? CameraModel { get; set; }
    public string? Lens { get; set; }
    public int? Iso { get; set; }
    public string? Aperture { get; set; }
    public string? ShutterSpeed { get; set; }
    public int? FocalLength { get; set; }
    public int? Orientation { get; set; }
    public string? Caption { get; set; }
    public long FolderId { get; set; }
    public Folder Folder { get; set; } = null!;
    public string? ThumbnailPath { get; set; }
    public string? ModelVersion { get; set; }
    public string? LastError { get; set; }

    public ICollection<Face> Faces { get; set; } = new List<Face>();
    public ICollection<ObjectDetection> Objects { get; set; } = new List<ObjectDetection>();
    public ICollection<SceneLabel> Scenes { get; set; } = new List<SceneLabel>();
    public ICollection<Embedding> Embeddings { get; set; } = new List<Embedding>();
    public ICollection<VideoFrame> VideoFrames { get; set; } = new List<VideoFrame>();
    public ICollection<MediaTag> Tags { get; set; } = new List<MediaTag>();
    public ICollection<OcrResult> OcrResults { get; set; } = new List<OcrResult>();
}
