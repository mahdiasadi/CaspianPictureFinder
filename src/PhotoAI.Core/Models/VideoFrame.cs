namespace PhotoAI.Core.Models;

public class VideoFrame
{
    public long Id { get; set; }
    public long MediaItemId { get; set; }
    public MediaItem MediaItem { get; set; } = null!;
    public double TimestampSeconds { get; set; }
    public string? FramePath { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public DateTime DateExtracted { get; set; }
    public bool IsKeyframe { get; set; }

    public ICollection<Embedding> Embeddings { get; set; } = new List<Embedding>();
}
