namespace PhotoAI.Core.Models;

public class Folder
{
    public long Id { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool RecursiveScan { get; set; } = true;
    public DateTime DateAdded { get; set; }
    public DateTime? LastScanned { get; set; }
    public long MediaCount { get; set; }

    public ICollection<MediaItem> MediaItems { get; set; } = new List<MediaItem>();
}
