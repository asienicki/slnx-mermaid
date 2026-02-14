using System.IO;

namespace SlnxMermaid.Core.Extensions
{
public static class FilePathExtensions
{
    public static string ToAbsolute(this string path, string baseDir)
    {
        if (Path.IsPathRooted(path))
            return Path.GetFullPath(path);

        return Path.GetFullPath(Path.Combine(baseDir, path));
    }

    public static string ResolveBaseDirectory(this string configPath)
    {
        if (string.IsNullOrWhiteSpace(configPath))
            return Directory.GetCurrentDirectory();

        return Path.GetDirectoryName(
            Path.GetFullPath(configPath));
    }
}
}
