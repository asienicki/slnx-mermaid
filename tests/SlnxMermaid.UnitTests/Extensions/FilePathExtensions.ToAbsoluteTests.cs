using SlnxMermaid.Core.Extensions;

namespace SlnxMermaid.Core.Tests.Extensions;

public class FilePathExtensionsToAbsoluteTests
{
    [Fact]
    public void ToAbsolute_WhenPathIsAbsolute_ShouldReturnNormalizedAbsolutePath()
    {
        var absolutePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "file.txt"));
        var baseDir = Directory.GetCurrentDirectory();

        var result = absolutePath.ToAbsolute(baseDir);

        Assert.Equal(Path.GetFullPath(absolutePath), result);
    }

    [Fact]
    public void ToAbsolute_WhenPathIsRelative_ShouldCombineWithBaseDir()
    {
        var baseDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        var relativePath = Path.Combine("folder", "file.txt");
        var expected = Path.GetFullPath(Path.Combine(baseDir, relativePath));

        var result = relativePath.ToAbsolute(baseDir);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToAbsolute_WhenRelativeContainsParentSegments_ShouldNormalizeCorrectly()
    {
        var baseDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "a", "b"));
        var relativePath = Path.Combine("..", "file.txt");
        var expected = Path.GetFullPath(Path.Combine(baseDir, relativePath));

        var result = relativePath.ToAbsolute(baseDir);

        Assert.Equal(expected, result);
    }
}
