using SlnxMermaid.Core.Config;
using SlnxMermaid.Core.Emit;
using SlnxMermaid.Core.Filtering;
using SlnxMermaid.Core.Graph;
using SlnxMermaid.Core.Naming;

namespace SlnxMermaid.Core.Tests.Emit;

public class MermaidEmitterEmitTests
{
    [Fact]
    public void Emit_WhenNoNodes_ShouldReturnOnlyGraphHeader()
    {
        var emitter = CreateEmitter();

        var result = emitter.Emit([], "TD");

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

        var result = emitter.Emit([a, b, c], "LR");

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

        var result = emitter.Emit([source, excluded], "TD");

        Assert.Equal($"graph TD{Environment.NewLine}", result);
    }

    private static MermaidEmitter CreateEmitter() =>
        new(
            new NameTransformer(new NamingConfig()),
            new ProjectFilter([]));
}
