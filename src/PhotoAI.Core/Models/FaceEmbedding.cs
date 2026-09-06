namespace PhotoAI.Core.Models;

public class FaceEmbedding
{
    public long Id { get; set; }
    public long FaceId { get; set; }
    public Face Face { get; set; } = null!;
    public string ModelName { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = string.Empty;
    public byte[] Vector { get; set; } = Array.Empty<byte>();
    public DateTime DateCreated { get; set; }
}
