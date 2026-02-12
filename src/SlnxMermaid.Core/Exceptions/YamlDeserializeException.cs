namespace SlnxMermaid.CLI.Exceptions
{
    public sealed class YamlDeserializeException : Exception
    {
        public string FilePath { get; }

        public YamlDeserializeException(string filePath) : base($"Config file is invalid: {filePath}")
        {
            FilePath = filePath;
        }
    }
    
}
