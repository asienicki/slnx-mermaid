using Microsoft.Build.Locator;
﻿using SlnxMermaid.Core.Graph;

namespace SlnxMermaid.Core.Tests.Graph;

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
            var projectB = Path.Combine(root, "Project.B", "Project.B.csproj");
            Directory.CreateDirectory(Path.GetDirectoryName(projectB)!);
            File.WriteAllText(projectB, """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
""");

            var projectA = Path.Combine(root, "Project.A", "Project.A.csproj");
            Directory.CreateDirectory(Path.GetDirectoryName(projectA)!);
            File.WriteAllText(projectA, $"""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..{Path.DirectorySeparatorChar}Project.B{Path.DirectorySeparatorChar}Project.B.csproj" />
  </ItemGroup>
</Project>
""");

            var solution = Path.Combine(root, "sample.slnx");
            File.WriteAllText(solution, $"""
<Solution>
  <Project Path="Project.A{Path.DirectorySeparatorChar}Project.A.csproj" />
  <Project Path="Project.B{Path.DirectorySeparatorChar}Project.B.csproj" />
</Solution>
""");

            var nodes = SolutionGraphAnalyzer.Analyze(solution);
            var a = Assert.Single(nodes.Where(n => n.Id == "Project_A"));
            var b = Assert.Single(nodes.Where(n => n.Id == "Project_B"));

            Assert.Contains(b, a.Dependencies);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
