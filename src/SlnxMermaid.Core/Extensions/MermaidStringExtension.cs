using System.IO;

namespace SlnxMermaid.Core.Extensions
{
    public static class MermaidStringExtension
    {
        public static string ConvertToAllowedMermaidString(this string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            var buffer = new char[name.Length];
            var index = 0;

            foreach (var c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    buffer[index++] = c;
                }
                else if (c == '.' || c == ' ')
                {
                    buffer[index++] = '_';
                }
            }

            return new string(buffer, 0, index);
        }

        public static string PrepareToDisplayOnMermaidDiagram(this string solutionName)
        {
            if (string.IsNullOrWhiteSpace(solutionName))
                return string.Empty;

            if (solutionName.Length > 0 && solutionName[0] == '.')
            {
                solutionName = solutionName.Substring(1);
            }


            return solutionName.ConvertToAllowedMermaidString();
        }
    }
}