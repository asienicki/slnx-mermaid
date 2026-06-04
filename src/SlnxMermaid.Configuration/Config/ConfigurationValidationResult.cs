using System.Collections.Generic;
using System.Linq;

namespace SlnxMermaid.Core.Config;

public sealed class ConfigurationValidationResult
{
    public ConfigurationValidationResult(IEnumerable<string> errors)
    {
        Errors = errors.ToArray();
    }

    public IReadOnlyList<string> Errors { get; }

    public bool IsValid => Errors.Count == 0;
}
