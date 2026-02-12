using SlnxMermaid.Core.Extensions;

namespace SlnxMermaid.Core.Tests.Extensions;

public class FilePathExtensionsTests
{
    [Fact]
    public void ToAbsolute_WhenPathIsAbsolute_ShouldReturnNormalizedAbsolutePath()
    {
        // Arrange
        var absolutePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "file.txt"));
        var baseDir = Directory.GetCurrentDirectory();

        // Act
        var result = absolutePath.ToAbsolute(baseDir);

        // Assert
        Assert.Equal(Path.GetFullPath(absolutePath), result);
    }

    [Fact]
    public void ToAbsolute_WhenPathIsRelative_ShouldCombineWithBaseDir()
    {
        // Arrange
        var baseDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        var relativePath = "folder/file.txt";

        var expected = Path.GetFullPath(Path.Combine(baseDir, relativePath));

        // Act
        var result = relativePath.ToAbsolute(baseDir);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToAbsolute_WhenRelativeContainsParentSegments_ShouldNormalizeCorrectly()
    {
        // Arrange
        var baseDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "a", "b"));
        var relativePath = "../file.txt";

        var expected = Path.GetFullPath(Path.Combine(baseDir, relativePath));

        // Act
        var result = relativePath.ToAbsolute(baseDir);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResolveBaseDirectory_WhenConfigPathIsNull_ShouldReturnCurrentDirectory()
    {
        // Act
        var result = ((string?)null).ResolveBaseDirectory();

        // Assert
        Assert.Equal(Directory.GetCurrentDirectory(), result);
    }

    [Fact]
    public void ResolveBaseDirectory_WhenConfigPathIsEmpty_ShouldReturnCurrentDirectory()
    {
        // Act
        var result = string.Empty.ResolveBaseDirectory();

        // Assert
        Assert.Equal(Directory.GetCurrentDirectory(), result);
    }

    [Fact]
    public void ResolveBaseDirectory_WhenConfigPathIsWhitespace_ShouldReturnCurrentDirectory()
    {
        // Act
        var result = "   ".ResolveBaseDirectory();

        // Assert
        Assert.Equal(Directory.GetCurrentDirectory(), result);
    }

    [Fact]
    public void ResolveBaseDirectory_WhenConfigPathIsRelative_ShouldReturnDirectoryOfFullPath()
    {
        // Arrange
        var relativePath = Path.Combine("configs", "appsettings.yml");
        var expected = Path.GetDirectoryName(Path.GetFullPath(relativePath));

        // Act
        var result = relativePath.ResolveBaseDirectory();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResolveBaseDirectory_WhenConfigPathIsAbsolute_ShouldReturnDirectory()
    {
        // Arrange
        var absolutePath = Path.GetFullPath(
            Path.Combine(Path.GetTempPath(), "configs", "appsettings.yml"));

        var expected = Path.GetDirectoryName(absolutePath);

        // Act
        var result = absolutePath.ResolveBaseDirectory();

        // Assert
        Assert.Equal(expected, result);
    }
}