using SlnxMermaid.Core.Extensions;

namespace SlnxMermaid.Core.Tests.Extensions;

public class FilePathExtensionsResolveBaseDirectoryTests
{
    [Fact]
    public void ResolveBaseDirectory_WhenConfigPathIsNull_ShouldReturnCurrentDirectory()
    {
        var result = FilePathExtensions.ResolveBaseDirectory(null!);

        Assert.Equal(Directory.GetCurrentDirectory(), result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveBaseDirectory_WhenConfigPathIsEmptyLike_ShouldReturnCurrentDirectory(string configPath)
    {
        var result = configPath.ResolveBaseDirectory();

        Assert.Equal(Directory.GetCurrentDirectory(), result);
    }

    [Fact]
    public void ResolveBaseDirectory_WhenConfigPathIsRelative_ShouldReturnDirectoryOfFullPath()
    {
        var relativePath = Path.Combine("configs", "appsettings.yml");
        var expected = Path.GetDirectoryName(Path.GetFullPath(relativePath));

        var result = relativePath.ResolveBaseDirectory();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResolveBaseDirectory_WhenConfigPathIsAbsolute_ShouldReturnDirectory()
    {
        var absolutePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "configs", "appsettings.yml"));
        var expected = Path.GetDirectoryName(absolutePath);

        var result = absolutePath.ResolveBaseDirectory();

        Assert.Equal(expected, result);
    }
}
