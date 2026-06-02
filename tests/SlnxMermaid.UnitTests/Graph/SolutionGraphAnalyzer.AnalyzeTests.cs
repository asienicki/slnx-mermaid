using Microsoft.Build.Exceptions;
using Microsoft.Build.Graph;
using Microsoft.Build.Locator;
using SlnxMermaid.Core.Config;
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

    [Fact]
    public void Analyze_WhenTransitiveDependencyOptionIsOmitted_ShouldOnlyCreateDirectDependencyEdges()
    {
        var root = Path.Combine(Path.GetTempPath(), $"slnx-mermaid-{Guid.NewGuid()}");
        Directory.CreateDirectory(root);

        try
        {
            var projectC = WriteProject(root, "Project.C");
            var projectB = WriteProject(root, "Project.B", projectC);
            var projectA = WriteProject(root, "Project.A", projectB);
            var solution = WriteSolution(root, projectA, projectB, projectC);

            var nodes = SolutionGraphAnalyzer.Analyze(solution);
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

    [Fact]
    public void Analyze_WhenConfigEnablesTransitiveDependencies_ShouldCreateIndirectDependencyEdge()
    {
        var root = Path.Combine(Path.GetTempPath(), $"slnx-mermaid-{Guid.NewGuid()}");
        Directory.CreateDirectory(root);

        try
        {
            var projectC = WriteProject(root, "Project.C");
            var projectB = WriteProject(root, "Project.B", projectC);
            var projectA = WriteProject(root, "Project.A", projectB);
            var solution = WriteSolution(root, projectA, projectB, projectC);
            var config = new SlnxMermaidConfig
            {
                Solution = solution,
                Diagram = new DiagramConfig
                {
                    IncludeTransitiveDependencies = true
                }
            };

            var nodes = SolutionGraphAnalyzer.Analyze(config);
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
    public void Analyze_WhenConfigIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SolutionGraphAnalyzer.Analyze((SlnxMermaidConfig)null!));
    }

    [Fact]
    public void GetTransitiveDependencies_WhenNodeHasNestedDependencies_ShouldReturnDirectAndIndirectDependencies()
    {
        var a = new ProjectNode("A", "A.csproj");
        var b = new ProjectNode("B", "B.csproj");
        var c = new ProjectNode("C", "C.csproj");
        a.Dependencies.Add(b);
        b.Dependencies.Add(c);

        var dependencies = SolutionGraphAnalyzer.GetTransitiveDependencies(a).ToList();

        Assert.Contains(b, dependencies);
        Assert.Contains(c, dependencies);
    }

    [Fact]
    public void AddTransitiveDependencies_WhenCycleExists_ShouldAddIndirectDependenciesWithoutSelfReference()
    {
        var a = new ProjectNode("A", "A.csproj");
        var b = new ProjectNode("B", "B.csproj");
        var c = new ProjectNode("C", "C.csproj");
        a.Dependencies.Add(b);
        b.Dependencies.Add(c);
        c.Dependencies.Add(a);

        SolutionGraphAnalyzer.AddTransitiveDependencies([a, b, c]);

        Assert.Contains(c, a.Dependencies);
        Assert.DoesNotContain(a, a.Dependencies);
    }

    [Fact]
    public void GetDirectProjectReferencePaths_WhenProjectHasReference_ShouldResolveReferencePath()
    {
        var root = Path.Combine(Path.GetTempPath(), $"slnx-mermaid-{Guid.NewGuid()}");
        Directory.CreateDirectory(root);

        try
        {
            var projectB = WriteProject(root, "Project.B");
            var projectA = WriteProject(root, "Project.A", projectB);
            var solution = WriteSolution(root, projectA, projectB);
            var graph = new ProjectGraph(solution);
            var node = Assert.Single(
                graph.ProjectNodes,
                n => StringComparer.OrdinalIgnoreCase.Equals(n.ProjectInstance.FullPath, projectA));

            var references = SolutionGraphAnalyzer.GetDirectProjectReferencePaths(node).ToList();

            Assert.Contains(projectB, references, StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void AnalyzeSolutionFile_WhenSlnxContainsProjectReference_ShouldCreateDependencyEdgeWithoutMsBuildEvaluation()
    {
        var root = Path.Combine(Path.GetTempPath(), $"slnx-mermaid-{Guid.NewGuid()}");
        Directory.CreateDirectory(root);

        try
        {
            var projectB = WriteProject(root, "Project.B");
            var projectA = WriteProject(root, "Project.A", projectB);
            var solution = WriteSolution(root, projectA, projectB);

            var nodes = SolutionGraphAnalyzer.AnalyzeSolutionFile(solution, includeTransitiveDependencies: false);
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
    public void AnalyzeSolutionFile_WhenSlnContainsProjectReference_ShouldCreateDependencyEdgeWithoutMsBuildEvaluation()
    {
        var root = Path.Combine(Path.GetTempPath(), $"slnx-mermaid-{Guid.NewGuid()}");
        Directory.CreateDirectory(root);

        try
        {
            var projectB = WriteProject(root, "Project.B");
            var projectA = WriteProject(root, "Project.A", projectB);
            var solution = WriteLegacySolution(root, projectA, projectB);

            var nodes = SolutionGraphAnalyzer.AnalyzeSolutionFile(solution, includeTransitiveDependencies: false);
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
    public void IsProjectEvaluationFailure_WhenAggregateContainsInvalidProjectFileException_ShouldReturnTrue()
    {
        var exception = new AggregateException(new InvalidProjectFileException("sample.csproj"));

        var result = SolutionGraphAnalyzer.IsProjectEvaluationFailure(exception);

        Assert.True(result);
    }

    [Fact]
    public void ToId_WhenProjectNameContainsSeparators_ShouldNormalizeName()
    {
        var result = SolutionGraphAnalyzer.ToId(Path.Combine("src", "Company.Project-Api.csproj"));

        Assert.Equal("Company_Project_Api", result);
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

    private static string WriteLegacySolution(string root, params string[] projects)
    {
        var solution = Path.Combine(root, "sample.sln");
        var projectEntries = string.Join(
            Environment.NewLine,
            projects.Select(project =>
                $"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{Path.GetFileNameWithoutExtension(project)}\", \"{Path.GetRelativePath(root, project)}\", \"{{{Guid.NewGuid()}}}\"{Environment.NewLine}EndProject"));

        File.WriteAllText(solution, $"""
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
{projectEntries}
Global
EndGlobal
""");

        return solution;
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
