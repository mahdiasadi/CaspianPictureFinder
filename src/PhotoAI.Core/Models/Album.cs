namespace PhotoAI.Core.Models;

public class Album
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSmartAlbum { get; set; }
    public string? SmartFilterJson { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime? DateModified { get; set; }
    public int MediaCount { get; set; }

    public ICollection<AlbumMedia> AlbumMediaItems { get; set; } = new List<AlbumMedia>();
}

public class AlbumMedia
{
    public long Id { get; set; }
    public long AlbumId { get; set; }
    public Album Album { get; set; } = null!;
    public long MediaItemId { get; set; }
    public MediaItem MediaItem { get; set; } = null!;
    public int SortOrder { get; set; }
    public DateTime DateAdded { get; set; }
}
