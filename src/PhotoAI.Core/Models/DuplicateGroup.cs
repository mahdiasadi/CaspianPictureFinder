namespace PhotoAI.Core.Models;

public class DuplicateGroup
{
    public long Id { get; set; }
    public DuplicateGroupType GroupType { get; set; }
    public float SimilarityScore { get; set; }
    public DateTime DateDetected { get; set; }

    public ICollection<DuplicateEntry> Entries { get; set; } = new List<DuplicateEntry>();
}

public class DuplicateEntry
{
    public long Id { get; set; }
    public long DuplicateGroupId { get; set; }
    public DuplicateGroup DuplicateGroup { get; set; } = null!;
    public long MediaItemId { get; set; }
    public MediaItem MediaItem { get; set; } = null!;
    public bool IsSelected { get; set; }
}
