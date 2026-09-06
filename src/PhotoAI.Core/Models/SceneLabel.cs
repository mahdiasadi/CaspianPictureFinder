namespace PhotoAI.Core.Models;

public class SceneLabel
{
    public long Id { get; set; }
    public long MediaItemId { get; set; }
    public MediaItem MediaItem { get; set; } = null!;
    public string Label { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public string? ModelName { get; set; }
    public DateTime DateDetected { get; set; }
}
