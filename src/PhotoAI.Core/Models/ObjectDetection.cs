namespace PhotoAI.Core.Models;

public class ObjectDetection
{
    public long Id { get; set; }
    public long MediaItemId { get; set; }
    public MediaItem MediaItem { get; set; } = null!;
    public string Label { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public int BoundingBoxX { get; set; }
    public int BoundingBoxY { get; set; }
    public int BoundingBoxWidth { get; set; }
    public int BoundingBoxHeight { get; set; }
    public string? ModelName { get; set; }
    public DateTime DateDetected { get; set; }
}
