using System;
using System.IO;
using SlnxMermaid.CLI.Exceptions;
using SlnxMermaid.Core.Config;

namespace SlnxMermaid.Core.Extensions
{
public static class SlnxMermaidConfigExtensions
{
    public static SlnxMermaidConfig Validate(this SlnxMermaidConfig config)
    {
        if (!File.Exists(config.Solution))
            throw new SolutionNotFoundException(config.Solution);

        return config;
    }

    public static SlnxMermaidConfig Normalize(this SlnxMermaidConfig config, string configPath)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        var baseDir = configPath.ResolveBaseDirectory();

        if (!string.IsNullOrWhiteSpace(config.Solution))
            config.Solution = config.Solution.ToAbsolute(baseDir);

        if (config.Output != null && !string.IsNullOrWhiteSpace(config.Output.File))
            config.Output.File = config.Output.File.ToAbsolute(baseDir);

        var file = config.Output != null ? config.Output.File : null;

        if (file != null && file.Contains("{date}"))
        {
            config.Output.File = file.Replace(
                "{date}",
                DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss"));
        }

        return config;
    }
}
}
