using LouisAgent.Composition.Agents;
using LouisAgent.Core.Delegation.Addresses;

namespace LouisAgent.Core.Tests.Delegation.Addresses;

/// <summary>
/// Who may call whom (F127.3.1 step 5): an identity asks only the addresses its <c>Calls</c> names, and an identity
/// that names none calls nobody, whatever the model writes.
/// </summary>
public class AgentCallsTests
{
    /// <summary>An identity that may ask another identity in its voice, and one role of a third.</summary>
    private static readonly FakeIdentity Asker = new("asker", calls: ["alpha", "Beta.Cook"]);

    /// <summary>Whether the asker may call this address, and why not.</summary>
    private static (bool Allowed, string? Why) Ask(FakeIdentity asker, string identity, string? role = null) =>
        (AgentCalls.Allows(asker, new AgentAddress(identity, role), out string? why), why);

    /// <summary>An address the identity names is allowed, however its list writes the case.</summary>
    [TestCase("alpha", null)]
    [TestCase("beta", "cook")]
    public void AListedAddress_IsAllowed(string identity, string? role) =>
        Assert.That(Ask(Asker, identity, role), Is.EqualTo((true, (string?)null)));

    /// <summary>Any other address is refused, and the reason names the ones it may call.</summary>
    [TestCase("gamma", null)]
    [TestCase("alpha", "cook")]
    [TestCase("beta", null)]
    public void AnyOther_IsRefused_NamingTheOnesItMayCall(string identity, string? role)
    {
        var (allowed, why) = Ask(Asker, identity, role);

        Assert.That(allowed, Is.False);
        Assert.That(why, Does.Contain(new AgentAddress(identity, role).ToString()).And.Contain("alpha, beta.cook"));
    }

    /// <summary>An identity that names no one calls nobody.</summary>
    [Test]
    public void AnIdentityWithNoCalls_CallsNobody()
    {
        var (allowed, why) = Ask(new FakeIdentity("loner"), "alpha");

        Assert.That(allowed, Is.False);
        Assert.That(why, Does.Contain("loner").And.Contain("(none)"));
    }
}
