using Microsoft.Build.Locator;
using SlnxMermaid.Core.Graph;

namespace SlnxMermaid.UnitTests.Graph;

public class SolutionGraphAnalyzerAnalyzeTests
{
    public SolutionGraphAnalyzerAnalyzeTests()
    {
        if (!MSBuildLocator.IsRegistered)
            MSBuildLocator.RegisterDefaults();
    }

    [Fact]
    public void Analyze_WhenSolutionContainsProjectReference_ShouldCreateDependencyEdge()
    {
        var root = Path.Combine(Path.GetTempPath(), $"slnx-mermaid-{Guid.NewGuid()}");
        Directory.CreateDirectory(root);

        try
        {
            var projectB = WriteProject(root, "Project.B");
            var projectA = WriteProject(root, "Project.A", projectB);
            var solution = WriteSolution(root, projectA, projectB);

            var nodes = SolutionGraphAnalyzer.Analyze(solution);
            var a = Assert.Single(nodes, n => n.Id == "Project_A");
            var b = Assert.Single(nodes, n => n.Id == "Project_B");

            Assert.Contains(b, a.Dependencies);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Analyze_WhenTransitiveDependenciesAreIncluded_ShouldCreateIndirectDependencyEdge()
    {
        var root = Path.Combine(Path.GetTempPath(), $"slnx-mermaid-{Guid.NewGuid()}");
        Directory.CreateDirectory(root);

        try
        {
            var projectC = WriteProject(root, "Project.C");
            var projectB = WriteProject(root, "Project.B", projectC);
            var projectA = WriteProject(root, "Project.A", projectB);
            var solution = WriteSolution(root, projectA, projectB, projectC);

            var nodes = SolutionGraphAnalyzer.Analyze(solution, includeTransitiveDependencies: true);
            var a = Assert.Single(nodes, n => n.Id == "Project_A");
            var b = Assert.Single(nodes, n => n.Id == "Project_B");
            var c = Assert.Single(nodes, n => n.Id == "Project_C");

            Assert.Contains(b, a.Dependencies);
            Assert.Contains(c, a.Dependencies);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Analyze_WhenTransitiveDependenciesAreExcluded_ShouldOnlyCreateDirectDependencyEdges()
    {
        var root = Path.Combine(Path.GetTempPath(), $"slnx-mermaid-{Guid.NewGuid()}");
        Directory.CreateDirectory(root);

        try
        {
            var projectC = WriteProject(root, "Project.C");
            var projectB = WriteProject(root, "Project.B", projectC);
            var projectA = WriteProject(root, "Project.A", projectB);
            var solution = WriteSolution(root, projectA, projectB, projectC);

            var nodes = SolutionGraphAnalyzer.Analyze(solution, includeTransitiveDependencies: false);
            var a = Assert.Single(nodes, n => n.Id == "Project_A");
            var b = Assert.Single(nodes, n => n.Id == "Project_B");
            var c = Assert.Single(nodes, n => n.Id == "Project_C");

            Assert.Contains(b, a.Dependencies);
            Assert.DoesNotContain(c, a.Dependencies);
            Assert.Contains(c, b.Dependencies);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string WriteProject(
        string root,
        string projectName,
        params string[] projectReferences)
    {
        var projectPath = Path.Combine(root, projectName, $"{projectName}.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(projectPath)!);

        var references = string.Join(
            Environment.NewLine,
            projectReferences.Select(reference =>
                $"    <ProjectReference Include=\"{Path.GetRelativePath(Path.GetDirectoryName(projectPath)!, reference)}\" />"));

        var itemGroup = projectReferences.Length == 0
            ? string.Empty
            : $"""
  <ItemGroup>
{references}
  </ItemGroup>
""";

        File.WriteAllText(projectPath, $"""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
{itemGroup}</Project>
""");

        return projectPath;
    }

    private static string WriteSolution(string root, params string[] projects)
    {
        var solution = Path.Combine(root, "sample.slnx");
        var projectEntries = string.Join(
            Environment.NewLine,
            projects.Select(project =>
                $"  <Project Path=\"{Path.GetRelativePath(root, project)}\" />"));

        File.WriteAllText(solution, $"""
<Solution>
{projectEntries}
</Solution>
""");

        return solution;
    }
}
