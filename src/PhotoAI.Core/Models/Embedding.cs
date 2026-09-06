namespace PhotoAI.Core.Models;

public class Embedding
{
    public long Id { get; set; }
    public long MediaItemId { get; set; }
    public MediaItem MediaItem { get; set; } = null!;
    public string ModelName { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = string.Empty;
    public string EmbeddingType { get; set; } = string.Empty;
    public byte[] Vector { get; set; } = Array.Empty<byte>();
    public int Dimension { get; set; }
    public DateTime DateCreated { get; set; }
}
