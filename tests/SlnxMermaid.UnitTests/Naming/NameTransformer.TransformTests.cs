using SlnxMermaid.Core.Config;
using SlnxMermaid.Core.Naming;

namespace SlnxMermaid.UnitTests.Naming;

public class NameTransformerTransformTests
{
    [Fact]
    public void Reported_Issue_By_MK_Test()
    {
        var transformer = new NameTransformer(new NamingConfig
        {
            StripPrefix = "SolutionNamePart1.SolutionNamePart2.SolutionNamePart3.SolutionNamePart4.",
        });

        var result = transformer.Transform("SolutionNamePart1.SolutionNamePart2.SolutionNamePart3.SolutionNamePart4.SolutionNamePart5");

        Assert.Equal("SolutionNamePart5", result);
    }
    [Fact]
    public void Reported_Issue_By_MK_Test2()
    {
        var transformer = new NameTransformer(new NamingConfig
        {
            StripPrefix = "SolutionNamePart1.SolutionNamePart2.SolutionNamePart3.SolutionNamePart4_",
        });

        var result = transformer.Transform("SolutionNamePart1.SolutionNamePart2.SolutionNamePart3.SolutionNamePart4.SolutionNamePart5");

        Assert.Equal("SolutionNamePart5", result);
    }

    [Fact]
    public void Transform_WhenPrefixMatches_ShouldStripPrefix()
    {
        var transformer = new NameTransformer(new NamingConfig
        {
            StripPrefix = "My.",
        });

        var result = transformer.Transform("My.Service");

        Assert.Equal("Service", result);
    }

    [Fact]
    public void Transform_WhenAliasExistsAfterPrefixStrip_ShouldReturnAlias()
    {
        var transformer = new NameTransformer(new NamingConfig
        {
            StripPrefix = "My.",
            Aliases = new Dictionary<string, string>
            {
                ["Service"] = "Svc"
            }
        });

        var result = transformer.Transform("My.Service");

        Assert.Equal("Svc", result);
    }

    [Fact]
    public void Transform_WhenPrefixDoesNotMatch_ShouldReturnOriginalOrAlias()
    {
        var transformer = new NameTransformer(new NamingConfig
        {
            StripPrefix = "Other.",
            Aliases = new Dictionary<string, string>
            {
                ["My_Service"] = "Svc"
            }
        });

        var result = transformer.Transform("My.Service");

        Assert.Equal("Svc", result);
    }
}
