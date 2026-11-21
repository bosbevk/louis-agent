using LouisAgent.Api.Config;

namespace LouisAgent.Api.Tests.Config;

/// <summary>
/// A site names where it lives with SITE_ADDRESS (FX216). Only a whole https address counts, so a typo can't send every
/// visitor somewhere odd; without one the site keeps answering on whatever address it was asked at.
/// </summary>
public class SiteAddressOptionsTests
{
    /// <summary>A whole https address is the site's, ending in a slash so a page's route joins it.</summary>
    [TestCase("https://louis-agent.dev", "https://louis-agent.dev/")]
    [TestCase(" https://framework.louis-agent.dev/ ", "https://framework.louis-agent.dev/")]
    public void AnHttpsAddress_IsTheSites(string setting, string expected)
    {
        var options = SiteAddressOptions.FromEnvironment(name => name == "SITE_ADDRESS" ? setting : null);

        Assert.That(options.Address, Is.EqualTo(new Uri(expected)));
    }

    /// <summary>Unset, blank, a bare host, plain http or an address with a path: no address, nothing changes.</summary>
    [TestCase(null)]
    [TestCase("  ")]
    [TestCase("louis-agent.dev")]
    [TestCase("http://louis-agent.dev")]
    [TestCase("https://louis-agent.dev/home")]
    public void AnythingElse_IsNoAddress(string? setting)
    {
        var options = SiteAddressOptions.FromEnvironment(_ => setting);

        Assert.That(options.Address, Is.Null);
    }
}
