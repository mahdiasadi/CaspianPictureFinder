using PhotoAI.Core.Enums;
using PhotoAI.Core.Models;
using Xunit;

namespace PhotoAI.Core.Tests;

public class ModelTests
{
    [Fact]
    public void MediaItem_DefaultValues()
    {
        var item = new MediaItem();
        Assert.Equal(string.Empty, item.FilePath);
        Assert.Equal(string.Empty, item.FileName);
        Assert.Equal(MediaType.Unknown, item.MediaType);
        Assert.Equal(MediaStatus.Pending, item.Status);
        Assert.NotNull(item.Faces);
        Assert.NotNull(item.Objects);
        Assert.NotNull(item.Scenes);
        Assert.NotNull(item.Embeddings);
    }

    [Fact]
    public void Folder_DefaultValues()
    {
        var folder = new Folder();
        Assert.Equal(string.Empty, folder.Path);
        Assert.True(folder.IsEnabled);
        Assert.True(folder.RecursiveScan);
    }

    [Fact]
    public void Person_DefaultValues()
    {
        var person = new Person();
        Assert.Equal(string.Empty, person.Name);
        Assert.NotNull(person.Faces);
    }

    [Fact]
    public void DuplicateGroup_DefaultValues()
    {
        var group = new DuplicateGroup();
        Assert.Equal(DuplicateGroupType.Exact, group.GroupType);
        Assert.NotNull(group.Entries);
    }

    [Fact]
    public void IndexProgress_DefaultValues()
    {
        var progress = new IndexProgress();
        Assert.Equal(0, progress.TotalFiles);
        Assert.Equal(string.Empty, progress.CurrentPhase);
        Assert.Equal(TimeSpan.Zero, progress.EstimatedTimeRemaining);
    }

    [Fact]
    public void SearchResult_DefaultValues()
    {
        var result = new SearchResult();
        Assert.Equal(0f, result.Score);
        Assert.Null(result.MatchReason);
    }
}
