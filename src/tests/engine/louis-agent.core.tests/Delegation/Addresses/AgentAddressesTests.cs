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

    /// <summary>A role reaches the specialist whose file is named for the identity.</summary>
    [Test]
    public void ARole_ReachesItsFile() =>
        Assert.That(Resolve("testprod", "specialist").Agent,
            Is.EqualTo(new AddressedAgent(new AgentAddress("testprod", "specialist"), AddressKind.Specialist,
                "testprod-specialist")));

    /// <summary>
    /// A minion is no agent to ask (F127.5): its file's name written as a role reaches no one here, whatever it names.
    /// </summary>
    [Test]
    public void AMinionsFile_IsNoRoleToAsk() =>
        Assert.That(Resolve("testprod", "minion-lookup").Agent, Is.Null);

    /// <summary>
    /// A minion's address (F127.5) is its identity and its function, <c>testprod.lookup</c>, which the minion tool
    /// resolves to the file <c>testprod-minion-lookup</c>.
    /// </summary>
    [Test]
    public void AMinionsAddress_ReachesItsFile()
    {
        var address = new AgentAddress("testprod", "lookup");

        Assert.Multiple(() =>
        {
            Assert.That(AgentAddresses.ResolveMinion(address, Identities, out _),
                Is.EqualTo(new AddressedAgent(address, AddressKind.Minion, "testprod-minion-lookup")));
            Assert.That(AgentAddresses.IsMinion(address), Is.True);
            Assert.That(AgentAddresses.IsMinion(new AgentAddress("testprod", "specialist")), Is.False);
        });
    }

    /// <summary>
    /// A minion address with no such minion is refused, naming the identity's minions by their addresses.
    /// </summary>
    [Test]
    public void AnUnknownMinion_IsRefused_NamingTheIdentitysMinions()
    {
        var agent = AgentAddresses.ResolveMinion(new AgentAddress("testprod", "cook"), Identities, out string? why);

        Assert.Multiple(() =>
        {
            Assert.That(agent, Is.Null);
            Assert.That(why, Does.Contain("'cook'").And.Contain("testprod.lookup"));
        });
    }

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
        Assert.That(why, Does.Contain($"'{role}'").And.Contain("testprod.specialist").And.Not.Contain("minion-lookup"));
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
