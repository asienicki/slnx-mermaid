namespace SlnxMermaid.CLI.Exceptions
{
    public sealed class ConfigurationFileNotFoundException : Exception
    {
        public string FilePath { get; }

        public ConfigurationFileNotFoundException(string filePath) : base($"Configuration file not found: {filePath}")
        { 
            FilePath = filePath;
        }
    }
    
}
