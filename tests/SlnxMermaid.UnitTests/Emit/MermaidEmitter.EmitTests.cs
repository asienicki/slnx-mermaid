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

        var result = emitter.Emit([], CreateConfig("TD"));

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

        var result = emitter.Emit([a, b, c], CreateConfig("LR"));

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

        var result = emitter.Emit([source, excluded], CreateConfig("TD"));

        Assert.Equal($"graph TD{Environment.NewLine}", result);
    }

    [Fact]
    public void Emit_WhenDepthOrderingIsDisabled_ShouldPreserveLegacyAlphabeticalEdgeOrdering()
    {
        var minimalApi = new ProjectNode("MinimalAPi", "MinimalAPi.csproj");
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

        var result = emitter.Emit([seeder, domain, infrastructure, dataAccess, minimalApi, application], CreateConfig("TD"));

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
    public void Emit_WhenDepthOrderingIsEnabled_ShouldWalkLongestDependencyChainsBeforeShorterRoots()
    {
        var minimalApi = new ProjectNode("MinimalAPi", "MinimalAPi.csproj");
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

        var result = emitter.Emit([seeder, domain, infrastructure, dataAccess, minimalApi, application], CreateConfig("TD", orderDependenciesByDepth: true));

        var expected =
            $"graph TD{Environment.NewLine}" +
            $"{Environment.NewLine}" +
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
        Assert.StartsWith(
            $"graph TD{Environment.NewLine}{Environment.NewLine}    MinimalApi -->",
            result,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_WhenDepthOrderingIsEnabled_ShouldCompleteDeeperRootChainBeforeShallowerRoot()
    {
        var a = new ProjectNode("A", "A.csproj");
        var b = new ProjectNode("B", "B.csproj");
        var c = new ProjectNode("C", "C.csproj");
        var d = new ProjectNode("D", "D.csproj");
        var e = new ProjectNode("E", "E.csproj");

        a.Dependencies.Add(b);
        b.Dependencies.Add(c);
        c.Dependencies.Add(d);
        e.Dependencies.Add(c);

        var emitter = CreateEmitter();

        var result = emitter.Emit([e, d, c, b, a], CreateConfig("TD", orderDependenciesByDepth: true));

        var expected =
            $"graph TD{Environment.NewLine}" +
            $"{Environment.NewLine}" +
            $"    A --> B{Environment.NewLine}" +
            $"{Environment.NewLine}" +
            $"    B --> C{Environment.NewLine}" +
            $"{Environment.NewLine}" +
            $"    C --> D{Environment.NewLine}" +
            $"{Environment.NewLine}" +
            $"    E --> C{Environment.NewLine}";

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Emit_WhenNodesIsNull_ShouldThrowArgumentNullException()
    {
        var emitter = CreateEmitter();

        var ex = Assert.Throws<ArgumentNullException>(() => emitter.Emit(null!, CreateConfig("TD")));

        Assert.Equal("nodes", ex.ParamName);
    }

    [Fact]
    public void Emit_WhenConfigIsNull_ShouldThrowArgumentNullException()
    {
        var emitter = CreateEmitter();

        var ex = Assert.Throws<ArgumentNullException>(() => emitter.Emit([], null!));

        Assert.Equal("config", ex.ParamName);
    }

    [Fact]
    public void Emit_WhenDiagramConfigIsNull_ShouldThrowArgumentNullException()
    {
        var emitter = CreateEmitter();
        var config = new SlnxMermaidConfig
        {
            Diagram = null!,
            Ui = null!
        };

        var ex = Assert.Throws<ArgumentNullException>(() => emitter.Emit([], config));

        Assert.Equal("diagram", ex.ParamName);
    }

    private static SlnxMermaidConfig CreateConfig(
        string direction,
        bool orderDependenciesByDepth = false) =>
        new()
        {
            Diagram = new DiagramConfig
            {
                Direction = direction,
                OrderDependenciesByDepth = orderDependenciesByDepth
            },
            Ui = null!
        };

    private static MermaidEmitter CreateEmitter() =>
        new(
            new NameTransformer(new NamingConfig()),
            new ProjectFilter([]));
}
