namespace SlnxMermaid.Core.Filtering;

public sealed class ProjectFilter
{
    private readonly string[] _excluded;

    public ProjectFilter(IEnumerable<string> excluded)
    {
        _excluded = excluded
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();
    }

    public bool IsAllowed(string projectId)
        => _excluded.All(x =>
            projectId.IndexOf(x, StringComparison.OrdinalIgnoreCase) < 0);
}
