using PhotoAI.Search;
using Xunit;

namespace PhotoAI.Core.Tests;

public class SearchRankerTests
{
    [Fact]
    public void SearchQuery_DefaultValues()
    {
        var query = new SearchQuery();
        Assert.Null(query.Text);
        Assert.Null(query.Objects);
        Assert.Null(query.Scenes);
        Assert.Null(query.PersonIds);
        Assert.Null(query.MinDate);
        Assert.Null(query.MaxDate);
    }

    [Fact]
    public void SearchQuery_WithText()
    {
        var query = new SearchQuery
        {
            Text = "red car",
            Objects = new[] { "car" },
            Scenes = new[] { "road" }
        };

        Assert.Equal("red car", query.Text);
        Assert.Single(query.Objects!);
        Assert.Single(query.Scenes!);
    }
}
