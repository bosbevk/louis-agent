using LouisAgent.Composition.Agents;
using LouisAgent.Composition.Packs;
using LouisAgent.Core.Delegation.Addresses;

namespace LouisAgent.Core.Tests.Delegation.Addresses;

/// <summary>
/// Who an address reaches on this instance (F127.3.1 step 4), on the tests' own prompts: <c>testprod</c> has a voice,
/// a specialist (<c>testprod-specialist</c>), a minion role (<c>testprod-minion-lookup</c>) and the minions' base
/// (<c>testprod-minion</c>), which is no role; <c>other</c> has no voice and no roles.
/// </summary>
public class AgentAddressesTests
{
    /// <summary>The two identities this instance runs.</summary>
    private static readonly IReadOnlyList<IAgentIdentity> Identities =
        [new FakeIdentity("testprod", voice: "testprod/voice-en"), new FakeIdentity("other")];

    /// <summary>What an address reaches, and the reason when it reaches no one.</summary>
    private static (AddressedAgent? Agent, string? Why) Resolve(string identity, string? role = null)
    {
        var agent = AgentAddresses.Resolve(new AgentAddress(identity, role), Identities, out string? why);
        return (agent, why);
    }

    /// <summary>An identity alone answers in its own voice.</summary>
    [Test]
    public void AnIdentity_AnswersInItsVoice() =>
        Assert.That(Resolve("testprod").Agent,
            Is.EqualTo(new AddressedAgent(new AgentAddress("testprod", null), AddressKind.Identity,
                "testprod/voice-en")));

    /// <summary>A role reaches the specialist or the minion role whose file is named for the identity.</summary>
    [TestCase("specialist", AddressKind.Specialist, "testprod-specialist")]
    [TestCase("minion-lookup", AddressKind.Minion, "testprod-minion-lookup")]
    public void ARole_ReachesItsFile(string role, AddressKind kind, string file) =>
        Assert.That(Resolve("testprod", role).Agent,
            Is.EqualTo(new AddressedAgent(new AgentAddress("testprod", role), kind, file)));

    /// <summary>An identity this instance doesn't run reaches no one, and the reason names the ones it does.</summary>
    [Test]
    public void AnUnknownIdentity_IsRefused_NamingTheOnesHere()
    {
        var (agent, why) = Resolve("nobody");

        Assert.That(agent, Is.Null);
        Assert.That(why, Does.Contain("nobody").And.Contain("other, testprod"));
    }

    /// <summary>
    /// A role with no file reaches no one, the minions' base included, and the reason names the identity's roles by
    /// their addresses.
    /// </summary>
    [TestCase("cook")]
    [TestCase("minion")]
    public void AnUnknownRole_IsRefused_NamingTheIdentitysRoles(string role)
    {
        var (agent, why) = Resolve("testprod", role);

        Assert.That(agent, Is.Null);
        Assert.That(why, Does.Contain($"'{role}'").And.Contain("testprod.minion-lookup, testprod.specialist"));
    }

    /// <summary>A role file belongs to the identity it is named for: another identity can't reach it.</summary>
    [Test]
    public void AnotherIdentitysRole_IsRefused()
    {
        var (agent, why) = Resolve("other", "specialist");

        Assert.That(agent, Is.Null);
        Assert.That(why, Does.Contain("other").And.Contain("'specialist'"));
    }

    /// <summary>An identity with no voice of its own can't be asked as itself.</summary>
    [Test]
    public void AnIdentityWithNoVoice_IsRefused()
    {
        var (agent, why) = Resolve("other");

        Assert.That(agent, Is.Null);
        Assert.That(why, Does.Contain("other"));
    }
}
