using SlnxMermaid.Core.Graph;

namespace SlnxMermaid.Core.Tests.Graph;

public class ProjectNodeConstructorTests
{
    [Fact]
    public void Constructor_ShouldAssignIdAndPath_AndInitializeDependencies()
    {
        var node = new ProjectNode("Project_A", "/tmp/project.csproj");

        Assert.Equal("Project_A", node.Id);
        Assert.Equal("/tmp/project.csproj", node.Path);
        Assert.NotNull(node.Dependencies);
        Assert.Empty(node.Dependencies);
    }
}
