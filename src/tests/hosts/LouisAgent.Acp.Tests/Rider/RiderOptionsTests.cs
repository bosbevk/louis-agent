using LouisAgent.Acp.Rider;

namespace LouisAgent.Acp.Tests.Rider;

/// <summary>Rider's settings, read here since CL57 and no longer by core: the defaults, and a false that turns discovery off.</summary>
public class RiderOptionsTests
{
    [Test]
    public void Unset_MeansRidersDefaultPort_DiscoveryOn_NoProjectPath() =>
        Assert.That(RiderOptions.FromEnvironment(_ => null),
            Is.EqualTo(new RiderOptions(RiderOptions.DefaultEndpoint, true, null)));

    [Test]
    public void TheSettings_AreRead()
    {
        var settings = new Dictionary<string, string?>
        {
            ["RIDER_MCP_ENDPOINT"] = "http://rider:1234/",
            ["RIDER_MCP_AUTO_DISCOVER"] = "false",
            ["RIDER_MCP_PROJECT_PATH"] = "/home/louis/repo",
        };

        Assert.That(RiderOptions.FromEnvironment(name => settings.GetValueOrDefault(name)),
            Is.EqualTo(new RiderOptions("http://rider:1234/", false, "/home/louis/repo")));
    }
}
