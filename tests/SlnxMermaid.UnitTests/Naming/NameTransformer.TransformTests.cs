using SlnxMermaid.Core.Config;
using SlnxMermaid.Core.Naming;

namespace SlnxMermaid.Core.Tests.Naming;

public class NameTransformerTransformTests
{
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
                ["My.Service"] = "Svc"
            }
        });

        var result = transformer.Transform("My.Service");

        Assert.Equal("Svc", result);
    }
}
