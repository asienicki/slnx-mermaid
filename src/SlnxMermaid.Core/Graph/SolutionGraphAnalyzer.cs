using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Build.Exceptions;
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
            try
            {
                var graph = new ProjectGraph(solutionPath);

                return AnalyzeProjectGraph(graph, includeTransitiveDependencies);
            }
            catch (Exception exception) when (IsProjectEvaluationFailure(exception))
            {
                return AnalyzeSolutionFile(solutionPath, includeTransitiveDependencies);
            }
        }

        internal static IReadOnlyCollection<ProjectNode> AnalyzeProjectGraph(
            ProjectGraph graph,
            bool includeTransitiveDependencies)
        {
            var nodes = graph.ProjectNodes
                .Select(n => new ProjectNode(
                    ToId(n.ProjectInstance.FullPath),
                    n.ProjectInstance.FullPath))
                .ToList();

            AddDirectDependencies(nodes, graph.ProjectNodes, GetDirectProjectReferencePaths);

            if (includeTransitiveDependencies)
                AddTransitiveDependencies(nodes);

            return nodes;
        }

        internal static IReadOnlyCollection<ProjectNode> AnalyzeSolutionFile(
            string solutionPath,
            bool includeTransitiveDependencies)
        {
            var projectPaths = GetSolutionProjectPaths(solutionPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var nodes = projectPaths
                .Select(projectPath => new ProjectNode(ToId(projectPath), projectPath))
                .ToList();

            AddDirectDependencies(nodes, projectPaths, GetDirectProjectReferencePaths);

            if (includeTransitiveDependencies)
                AddTransitiveDependencies(nodes);

            return nodes;
        }

        internal static void AddDirectDependencies<TProject>(
            IEnumerable<ProjectNode> nodes,
            IEnumerable<TProject> projects,
            Func<TProject, IEnumerable<string>> getDirectProjectReferencePaths)
        {
            var byPath = nodes
                .GroupBy(n => n.Path, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var project in projects)
            {
                ProjectNode from;
                var projectPath = GetProjectPath(project);

                if (!byPath.TryGetValue(projectPath, out from))
                    continue;

                foreach (var dependencyPath in getDirectProjectReferencePaths(project))
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
        }

        internal static bool IsProjectEvaluationFailure(Exception exception)
        {
            var aggregateException = exception as AggregateException;

            if (aggregateException != null)
                return aggregateException.Flatten().InnerExceptions.Any(IsProjectEvaluationFailure);

            return exception is InvalidProjectFileException;
        }

        internal static IEnumerable<string> GetSolutionProjectPaths(string solutionPath)
        {
            var extension = Path.GetExtension(solutionPath);

            if (StringComparer.OrdinalIgnoreCase.Equals(extension, ".slnx"))
                return GetSlnxProjectPaths(solutionPath);

            if (StringComparer.OrdinalIgnoreCase.Equals(extension, ".sln"))
                return GetSlnProjectPaths(solutionPath);

            return Enumerable.Empty<string>();
        }

        internal static IEnumerable<string> GetSlnxProjectPaths(string solutionPath)
        {
            var solutionDirectory = Path.GetDirectoryName(solutionPath);
            var document = XDocument.Load(solutionPath);

            return document
                .Descendants()
                .Where(element => StringComparer.OrdinalIgnoreCase.Equals(element.Name.LocalName, "Project"))
                .Select(element => GetAttributeValue(element, "Path"))
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => Path.GetFullPath(Path.Combine(solutionDirectory, path)));
        }

        internal static IEnumerable<string> GetSlnProjectPaths(string solutionPath)
        {
            var solutionDirectory = Path.GetDirectoryName(solutionPath);
            var projectPathPattern = new Regex(
                "^Project\\(\"[^\"]+\"\\)\\s*=\\s*\"[^\"]+\"\\s*,\\s*\"(?<path>[^\"]+\\.(?:csproj|fsproj|vbproj))\"",
                RegexOptions.IgnoreCase);

            return File.ReadLines(solutionPath)
                .Select(line => projectPathPattern.Match(line))
                .Where(match => match.Success)
                .Select(match => match.Groups["path"].Value)
                .Select(path => Path.GetFullPath(Path.Combine(solutionDirectory, path)));
        }

        internal static IEnumerable<string> GetDirectProjectReferencePaths(string projectPath)
        {
            var projectDirectory = Path.GetDirectoryName(projectPath);
            var document = XDocument.Load(projectPath);

            return document
                .Descendants()
                .Where(element => StringComparer.OrdinalIgnoreCase.Equals(element.Name.LocalName, ProjectReferenceItemName))
                .Select(element => GetAttributeValue(element, "Include"))
                .Where(include => !string.IsNullOrWhiteSpace(include))
                .Select(include => Path.GetFullPath(Path.Combine(projectDirectory, include)));
        }

        private static string GetProjectPath<TProject>(TProject project)
        {
            var node = project as ProjectGraphNode;

            if (node != null)
                return node.ProjectInstance.FullPath;

            return project as string;
        }

        private static string GetAttributeValue(XElement element, string name)
        {
            var attribute = element.Attributes()
                .FirstOrDefault(current => StringComparer.OrdinalIgnoreCase.Equals(current.Name.LocalName, name));

            return attribute == null ? null : attribute.Value;
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
