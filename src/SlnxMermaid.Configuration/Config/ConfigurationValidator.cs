using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace SlnxMermaid.Core.Config;

public interface IConfigurationValidator
{
    ConfigurationValidationResult Validate(SlnxMermaidConfig config, string? baseDirectory = null);
}

public sealed class ConfigurationValidator : IConfigurationValidator
{
    public ConfigurationValidationResult Validate(SlnxMermaidConfig config, string? baseDirectory = null)
    {
        var errors = new List<string>();

        if (config == null)
        {
            errors.Add("Configuration is missing.");
            return new ConfigurationValidationResult(errors);
        }

        if (string.IsNullOrWhiteSpace(config.Solution))
            errors.Add("Solution path is required.");
        else
        {
            var solutionPath = ResolvePath(config.Solution!, baseDirectory);
            if (!File.Exists(solutionPath))
                errors.Add($"Solution file does not exist: {config.Solution}");
        }

        if (config.Ui != null)
        {
            if (!string.IsNullOrWhiteSpace(config.Ui.Mode)
                && !string.Equals(config.Ui.Mode, "dark", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(config.Ui.Mode, "light", StringComparison.OrdinalIgnoreCase))
                errors.Add("UI mode must be 'dark' or 'light'.");

            ValidateDictionaryKeys(config.Ui.Semantic, "Ui.Semantic", errors);
            ValidateDictionaryKeys(config.Ui.Mappings, "Ui.Mappings", errors);
        }

        if (config.Naming != null)
            ValidateDictionaryKeys(config.Naming.Aliases, "Naming.Aliases", errors);

        return new ConfigurationValidationResult(errors);
    }

    private static string ResolvePath(string path, string? baseDirectory)
    {
        if (Path.IsPathRooted(path) || string.IsNullOrWhiteSpace(baseDirectory))
            return Path.GetFullPath(path);

        return Path.GetFullPath(Path.Combine(baseDirectory, path));
    }

    private static void ValidateDictionaryKeys(IDictionary? dictionary, string path, ICollection<string> errors)
    {
        if (dictionary == null)
            return;

        foreach (var key in dictionary.Keys)
        {
            if (key == null || string.IsNullOrWhiteSpace(key.ToString()))
                errors.Add($"{path} contains an empty key.");
        }
    }
}
