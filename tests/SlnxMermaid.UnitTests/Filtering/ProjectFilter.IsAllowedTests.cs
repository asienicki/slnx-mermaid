using SlnxMermaid.Core.Filtering;

namespace SlnxMermaid.Core.Tests.Filtering;

public class ProjectFilterIsAllowedTests
{
    [Fact]
    public void IsAllowed_WhenNoExclusions_ShouldReturnTrue()
    {
        var filter = new ProjectFilter([]);

        var result = filter.IsAllowed("My.Project");

        Assert.True(result);
    }

    [Fact]
    public void IsAllowed_WhenProjectContainsExcludedPart_ShouldReturnFalse()
    {
        var filter = new ProjectFilter(["Core"]);

        var result = filter.IsAllowed("My.Core.Project");

        Assert.False(result);
    }

    [Fact]
    public void IsAllowed_WhenExcludedValueIsDifferentCase_ShouldStillReturnFalse()
    {
        var filter = new ProjectFilter(["core"]);

        var result = filter.IsAllowed("My.Core.Project");

        Assert.False(result);
    }

    [Fact]
    public void IsAllowed_WhenExclusionEntriesAreEmpty_ShouldIgnoreThem()
    {
        var filter = new ProjectFilter(["", "   "]);

        var result = filter.IsAllowed("My.Project");

        Assert.True(result);
    }
}
