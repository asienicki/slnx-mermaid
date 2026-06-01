using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Graph;
using SlnxMermaid.Core.Config;

namespace SlnxMermaid.Core.Graph
{
    public static class SolutionGraphAnalyzer
    {
        private const string ProjectReferenceItemName = "ProjectReference";

        public static IReadOnlyCollection<ProjectNode> Analyze(SlnxMermaidConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            return Analyze(
                config.Solution,
                config.Diagram != null && config.Diagram.IncludeTransitiveDependencies);
        }

        public static IReadOnlyCollection<ProjectNode> Analyze(string solutionPath)
        {
            return Analyze(solutionPath, includeTransitiveDependencies: false);
        }

        public static IReadOnlyCollection<ProjectNode> Analyze(
            string solutionPath,
            bool includeTransitiveDependencies)
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

                foreach (var dependencyPath in GetDirectProjectReferencePaths(node))
                {
                    ProjectNode to;

                    if (byPath.TryGetValue(dependencyPath, out to))
                    {
                        if (!StringComparer.OrdinalIgnoreCase.Equals(from.Path, to.Path))
                        {
                            from.Dependencies.Add(to);
                        }
                    }
                }
            }

            if (includeTransitiveDependencies)
                AddTransitiveDependencies(nodes);

            return nodes;
        }

        internal static IEnumerable<string> GetDirectProjectReferencePaths(ProjectGraphNode node)
        {
            var projectDirectory = Path.GetDirectoryName(node.ProjectInstance.FullPath);

            foreach (var projectReference in node.ProjectInstance.GetItems(ProjectReferenceItemName))
            {
                var include = projectReference.EvaluatedInclude;

                if (string.IsNullOrWhiteSpace(include))
                    continue;

                yield return Path.GetFullPath(Path.Combine(projectDirectory, include));
            }
        }

        internal static void AddTransitiveDependencies(IEnumerable<ProjectNode> nodes)
        {
            foreach (var node in nodes)
            {
                foreach (var dependency in GetTransitiveDependencies(node))
                {
                    if (!StringComparer.OrdinalIgnoreCase.Equals(node.Path, dependency.Path))
                        node.Dependencies.Add(dependency);
                }
            }
        }

        internal static IEnumerable<ProjectNode> GetTransitiveDependencies(ProjectNode node)
        {
            var visited = new HashSet<ProjectNode>();
            var stack = new Stack<ProjectNode>(node.Dependencies);

            while (stack.Count > 0)
            {
                var dependency = stack.Pop();

                if (!visited.Add(dependency))
                    continue;

                yield return dependency;

                foreach (var transitiveDependency in dependency.Dependencies)
                    stack.Push(transitiveDependency);
            }
        }

        internal static string ToId(string projectPath)
        {
            return Path.GetFileNameWithoutExtension(projectPath)
                .Replace('.', '_')
                .Replace('-', '_');
        }
    }
}
