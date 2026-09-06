namespace PhotoAI.Core.Models;

public class Face
{
    public long Id { get; set; }
    public long MediaItemId { get; set; }
    public MediaItem MediaItem { get; set; } = null!;
    public long? PersonId { get; set; }
    public Person? Person { get; set; }
    public int BoundingBoxX { get; set; }
    public int BoundingBoxY { get; set; }
    public int BoundingBoxWidth { get; set; }
    public int BoundingBoxHeight { get; set; }
    public float Confidence { get; set; }
    public string? FaceThumbnailPath { get; set; }
    public DateTime DateDetected { get; set; }

    public ICollection<FaceEmbedding> Embeddings { get; set; } = new List<FaceEmbedding>();
}
