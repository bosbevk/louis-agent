using LouisAgent.Composition.Agents;

namespace LouisAgent.Core.Tests.Composition.Agents;

/// <summary>
/// An address is read once from what the model wrote (F127.3.1 step 4): <c>{identity}</c> or
/// <c>{identity}.{role}</c>, in lowercase letters, digits and dashes, however the model cased or spaced it.
/// </summary>
public class AgentAddressTests
{
    /// <summary>An identity alone, and an identity with one of its roles.</summary>
    [TestCase("alpha", "alpha", null)]
    [TestCase("alpha.beta", "alpha", "beta")]
    [TestCase("alpha-guide.minion-lookup", "alpha-guide", "minion-lookup")]
    [TestCase("alpha2.beta3", "alpha2", "beta3")]
    public void AnAddress_ReadsAsItsIdentityAndRole(string text, string identity, string? role)
    {
        Assert.That(AgentAddress.TryParse(text, out var address), Is.True);
        Assert.That(address, Is.EqualTo(new AgentAddress(identity, role)));
    }

    /// <summary>
    /// An address may name its server before a colon (F127.3.2): <c>partner:shop</c> is the shop on the server named
    /// partner, as <c>AGENT_PEERS</c> names it; without one it is this instance.
    /// </summary>
    [TestCase("partner:alpha", "partner", "alpha", null)]
    [TestCase("partner:alpha.beta", "partner", "alpha", "beta")]
    [TestCase(" Partner:Alpha ", "partner", "alpha", null)]
    public void AnAddress_MayNameItsServer(string text, string peer, string identity, string? role)
    {
        Assert.That(AgentAddress.TryParse(text, out var address), Is.True);
        Assert.That(address, Is.EqualTo(new AgentAddress(identity, role, peer)));
    }

    /// <summary>A server named without an identity, an empty one, or two of them, is no address.</summary>
    [TestCase(":alpha")]
    [TestCase("partner:")]
    [TestCase("a:b:alpha")]
    [TestCase("part ner:alpha")]
    public void ABadServer_IsNoAddress(string text) =>
        Assert.That(AgentAddress.TryParse(text, out _), Is.False);

    /// <summary>The case and the spaces around it don't matter: the model writes names in many ways.</summary>
    [Test]
    public void TheCaseAndTheSpacesAround_AreIgnored()
    {
        Assert.That(AgentAddress.TryParse(" Alpha.Beta ", out var address), Is.True);
        Assert.That(address, Is.EqualTo(new AgentAddress("alpha", "beta")));
    }

    /// <summary>Nothing, more than one dot, an empty part, or a character no name has, is no address.</summary>
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("alpha.beta.gamma")]
    [TestCase(".beta")]
    [TestCase("alpha.")]
    [TestCase("alpha beta")]
    [TestCase("alpha/voice")]
    [TestCase("alpha_beta")]
    [TestCase("-alpha")]
    [TestCase("alpha.-beta")]
    public void AnythingElse_IsNoAddress(string? text) =>
        Assert.That(AgentAddress.TryParse(text, out _), Is.False);

    /// <summary>An address writes back as it is read: the identity, then the role after a dot.</summary>
    [TestCase("alpha")]
    [TestCase("alpha.beta")]
    [TestCase("partner:alpha.beta")]
    public void AnAddress_WritesBackAsItWasRead(string text)
    {
        string? written = AgentAddress.TryParse(text, out var address) ? address.ToString() : null;

        Assert.That(written, Is.EqualTo(text));
    }
}
