using SlnxMermaid.Core.Config;
using SlnxMermaid.Core.Emit;
using SlnxMermaid.Core.Filtering;
using SlnxMermaid.Core.Graph;
using SlnxMermaid.Core.Naming;

namespace SlnxMermaid.UnitTests.Emit;

public class MermaidEmitterEmitTests
{
    [Fact]
    public void Emit_WhenNoNodes_ShouldReturnOnlyGraphHeader()
    {
        var emitter = CreateEmitter();

        var result = emitter.Emit([], CreateDiagram("TD"));

        Assert.Equal($"graph TD{Environment.NewLine}", result);
    }

    [Fact]
    public void Emit_WhenDependenciesExist_ShouldCreateSortedUniqueEdges()
    {
        var a = new ProjectNode("A", "A.csproj");
        var b = new ProjectNode("B", "B.csproj");
        var c = new ProjectNode("C", "C.csproj");
        a.Dependencies.Add(b);
        a.Dependencies.Add(c);
        a.Dependencies.Add(b);

        var emitter = CreateEmitter();

        var result = emitter.Emit([a, b, c], CreateDiagram("LR"));

        var expected =
            $"graph LR{Environment.NewLine}" +
            $"    A --> B{Environment.NewLine}" +
            $"    A --> C{Environment.NewLine}";

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Emit_WhenNodeOrDependencyFilteredOut_ShouldSkipEdge()
    {
        var source = new ProjectNode("Source", "s.csproj");
        var excluded = new ProjectNode("Excluded", "e.csproj");
        source.Dependencies.Add(excluded);

        var emitter = new MermaidEmitter(
            new NameTransformer(new NamingConfig()),
            new ProjectFilter(["Excluded"]));

        var result = emitter.Emit([source, excluded], CreateDiagram("TD"));

        Assert.Equal($"graph TD{Environment.NewLine}", result);
    }

    [Fact]
    public void Emit_WhenRoleOrderingIsDisabled_ShouldPreserveLegacyAlphabeticalEdgeOrdering()
    {
        var minimalApi = new ProjectNode("MinimalApi", "MinimalApi.csproj");
        var application = new ProjectNode("Application", "Application.csproj");
        var infrastructure = new ProjectNode("Infrastructure", "Infrastructure.csproj");
        var dataAccess = new ProjectNode("DataAccess", "DataAccess.csproj");
        var domain = new ProjectNode("Domain", "Domain.csproj");
        var seeder = new ProjectNode("Seeder", "Seeder.csproj");

        application.Dependencies.Add(domain);
        infrastructure.Dependencies.Add(application);
        infrastructure.Dependencies.Add(dataAccess);
        minimalApi.Dependencies.Add(application);
        minimalApi.Dependencies.Add(infrastructure);
        seeder.Dependencies.Add(dataAccess);

        var emitter = CreateEmitter();

        var result = emitter.Emit([seeder, domain, infrastructure, dataAccess, minimalApi, application], CreateDiagram("TD"));

        var expected =
            $"graph TD{Environment.NewLine}" +
            $"    Application --> Domain{Environment.NewLine}" +
            $"    Infrastructure --> Application{Environment.NewLine}" +
            $"    Infrastructure --> DataAccess{Environment.NewLine}" +
            $"    MinimalApi --> Application{Environment.NewLine}" +
            $"    MinimalApi --> Infrastructure{Environment.NewLine}" +
            $"    Seeder --> DataAccess{Environment.NewLine}";

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Emit_WhenGraphIsScrambled_ShouldOrderMainEntryPointsBeforeSecondaryEntryPoints()
    {
        var minimalApi = new ProjectNode("MinimalApi", "MinimalApi.csproj");
        var application = new ProjectNode("Application", "Application.csproj");
        var infrastructure = new ProjectNode("Infrastructure", "Infrastructure.csproj");
        var dataAccess = new ProjectNode("DataAccess", "DataAccess.csproj");
        var domain = new ProjectNode("Domain", "Domain.csproj");
        var seeder = new ProjectNode("Seeder", "Seeder.csproj");

        application.Dependencies.Add(domain);
        infrastructure.Dependencies.Add(application);
        infrastructure.Dependencies.Add(dataAccess);
        minimalApi.Dependencies.Add(application);
        minimalApi.Dependencies.Add(infrastructure);
        seeder.Dependencies.Add(dataAccess);

        var emitter = CreateEmitter();

        var result = emitter.Emit([seeder, domain, infrastructure, dataAccess, minimalApi, application], CreateDiagram("TD", orderDependenciesByRole: true));

        var expected =
            $"graph TD{Environment.NewLine}" +
            $"    MinimalApi --> Application{Environment.NewLine}" +
            $"    MinimalApi --> Infrastructure{Environment.NewLine}" +
            $"{Environment.NewLine}" +
            $"    Application --> Domain{Environment.NewLine}" +
            $"{Environment.NewLine}" +
            $"    Infrastructure --> Application{Environment.NewLine}" +
            $"    Infrastructure --> DataAccess{Environment.NewLine}" +
            $"{Environment.NewLine}" +
            $"    Seeder --> DataAccess{Environment.NewLine}";

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Emit_WhenDiagramConfigIsNull_ShouldThrowArgumentNullException()
    {
        var emitter = CreateEmitter();

        Assert.Throws<ArgumentNullException>(() => emitter.Emit([], null!));
    }

    private static DiagramConfig CreateDiagram(
        string direction,
        bool orderDependenciesByRole = false) =>
        new()
        {
            Direction = direction,
            OrderDependenciesByRole = orderDependenciesByRole
        };

    private static MermaidEmitter CreateEmitter() =>
        new(
            new NameTransformer(new NamingConfig()),
            new ProjectFilter([]));
}
