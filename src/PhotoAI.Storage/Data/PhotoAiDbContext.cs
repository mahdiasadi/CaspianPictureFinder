using Microsoft.EntityFrameworkCore;
using PhotoAI.Core.Models;

namespace PhotoAI.Storage.Data;

public class PhotoAiDbContext : DbContext
{
    public DbSet<MediaItem> MediaItems => Set<MediaItem>();
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Face> Faces => Set<Face>();
    public DbSet<FaceEmbedding> FaceEmbeddings => Set<FaceEmbedding>();
    public DbSet<ObjectDetection> ObjectDetections => Set<ObjectDetection>();
    public DbSet<SceneLabel> SceneLabels => Set<SceneLabel>();
    public DbSet<Embedding> Embeddings => Set<Embedding>();
    public DbSet<VideoFrame> VideoFrames => Set<VideoFrame>();
    public DbSet<MediaTag> MediaTags => Set<MediaTag>();
    public DbSet<OcrResult> OcrResults => Set<OcrResult>();
    public DbSet<DuplicateGroup> DuplicateGroups => Set<DuplicateGroup>();
    public DbSet<DuplicateEntry> DuplicateEntries => Set<DuplicateEntry>();
    public DbSet<IndexJob> IndexJobs => Set<IndexJob>();
    public DbSet<Album> Albums => Set<Album>();
    public DbSet<AlbumMedia> AlbumMediaItems => Set<AlbumMedia>();
    public DbSet<AppSettings> Settings => Set<AppSettings>();

    private readonly string _dbPath;

    public PhotoAiDbContext(string dbPath)
    {
        _dbPath = dbPath;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={_dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MediaItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.FilePath).IsUnique();
            entity.HasIndex(e => e.FilePathHash);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.MediaType);
            entity.HasIndex(e => e.FolderId);
            entity.HasIndex(e => e.ContentHash);
            entity.HasIndex(e => e.PerceptualHash);
            entity.HasIndex(e => e.DateTaken);
            entity.HasOne(e => e.Folder).WithMany(f => f.MediaItems).HasForeignKey(e => e.FolderId);
        });

        modelBuilder.Entity<Folder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.Path).IsUnique();
        });

        modelBuilder.Entity<Person>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<Face>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.MediaItemId);
            entity.HasIndex(e => e.PersonId);
            entity.HasOne(e => e.MediaItem).WithMany(m => m.Faces).HasForeignKey(e => e.MediaItemId);
            entity.HasOne(e => e.Person).WithMany(p => p.Faces).HasForeignKey(e => e.PersonId);
        });

        modelBuilder.Entity<FaceEmbedding>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.FaceId);
            entity.HasOne(e => e.Face).WithMany(f => f.Embeddings).HasForeignKey(e => e.FaceId);
        });

        modelBuilder.Entity<ObjectDetection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.MediaItemId);
            entity.HasIndex(e => e.Label);
            entity.HasOne(e => e.MediaItem).WithMany(m => m.Objects).HasForeignKey(e => e.MediaItemId);
        });

        modelBuilder.Entity<SceneLabel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.MediaItemId);
            entity.HasIndex(e => e.Label);
            entity.HasOne(e => e.MediaItem).WithMany(m => m.Scenes).HasForeignKey(e => e.MediaItemId);
        });

        modelBuilder.Entity<Embedding>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.MediaItemId);
            entity.HasIndex(e => new { e.ModelName, e.EmbeddingType });
            entity.HasOne(e => e.MediaItem).WithMany(m => m.Embeddings).HasForeignKey(e => e.MediaItemId);
        });

        modelBuilder.Entity<VideoFrame>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.MediaItemId);
            entity.HasOne(e => e.MediaItem).WithMany(m => m.VideoFrames).HasForeignKey(e => e.MediaItemId);
        });

        modelBuilder.Entity<MediaTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => new { e.MediaItemId, e.Tag });
            entity.HasOne(e => e.MediaItem).WithMany(m => m.Tags).HasForeignKey(e => e.MediaItemId);
        });

        modelBuilder.Entity<OcrResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.MediaItemId);
            entity.HasOne(e => e.MediaItem).WithMany(m => m.OcrResults).HasForeignKey(e => e.MediaItemId);
        });

        modelBuilder.Entity<DuplicateGroup>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<DuplicateEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.DuplicateGroupId);
            entity.HasIndex(e => e.MediaItemId);
            entity.HasOne(e => e.DuplicateGroup).WithMany(g => g.Entries).HasForeignKey(e => e.DuplicateGroupId);
            entity.HasOne(e => e.MediaItem).WithMany().HasForeignKey(e => e.MediaItemId);
        });

        modelBuilder.Entity<IndexJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<Album>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<AlbumMedia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => new { e.AlbumId, e.MediaItemId }).IsUnique();
            entity.HasOne(e => e.Album).WithMany(a => a.AlbumMediaItems).HasForeignKey(e => e.AlbumId);
            entity.HasOne(e => e.MediaItem).WithMany().HasForeignKey(e => e.MediaItemId);
        });

        modelBuilder.Entity<AppSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.Key).IsUnique();
        });
    }
}
