using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Graph;

namespace SlnxMermaid.Core.Graph
{
    public static class SolutionGraphAnalyzer
    {
        public static IReadOnlyCollection<ProjectNode> Analyze(string solutionPath)
        {
            var graph = new ProjectGraph(solutionPath);

            var nodes = graph.ProjectNodes
                .Select(n => new ProjectNode(
                    ToId(n.ProjectInstance.FullPath),
                    n.ProjectInstance.FullPath))
                .ToList();

            var byPath = nodes
                .GroupBy(n => n.Path, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var node in graph.ProjectNodes)
            {
                var from = byPath[node.ProjectInstance.FullPath];

                foreach (var dep in node.ProjectReferences)
                {
                    ProjectNode to;

                    if (byPath.TryGetValue(dep.ProjectInstance.FullPath, out to))
                    {
                        if (!StringComparer.OrdinalIgnoreCase.Equals(from.Path, to.Path))
                        {
                            from.Dependencies.Add(to);
                        }
                    }
                }
            }

            return nodes;
        }

        private static string ToId(string projectPath)
        {
            return Path.GetFileNameWithoutExtension(projectPath)
                .Replace('.', '_')
                .Replace('-', '_');
        }
    }
}
