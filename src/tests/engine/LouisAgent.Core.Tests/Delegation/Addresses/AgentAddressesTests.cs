using LouisAgent.Composition.Agents;
using LouisAgent.Composition.Packs;
using LouisAgent.Core.Delegation.Addresses;

namespace LouisAgent.Core.Tests.Delegation.Addresses;

/// <summary>
/// Who an address reaches on this instance (F127.3.1 step 4), on the tests' own prompts: <c>testprod</c> has a voice,
/// a subagent (<c>testprod.researcher</c>), a lookup agent (<c>testprod.lookup</c>) and the lookup agents' base
/// (<c>testprod-helper</c>), which is no role; <c>other</c> has no voice and no roles.
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

    /// <summary>
    /// A role reaches the identity's subagent, named by the address itself (CL68.2, ADR 0133): one name, no second
    /// spelling made from it.
    /// </summary>
    [Test]
    public void ARole_ReachesItsSubagent_NamedByTheAddress() =>
        Assert.That(Resolve("testprod", "researcher").Agent,
            Is.EqualTo(new AddressedAgent(new AgentAddress("testprod", "researcher"), AddressKind.Subagent,
                "testprod.researcher")));

    /// <summary>
    /// A lookup agent is no agent to ask (F127.5): its file's name written as a role reaches no one here, whatever it
    /// names.
    /// </summary>
    [Test]
    public void ALookupAgentsFile_IsNoRoleToAsk() =>
        Assert.That(Resolve("testprod", "page-lookup").Agent, Is.Null);

    /// <summary>
    /// A lookup agent's address (F127.5) is its identity and its function, <c>testprod.lookup</c>, and that address is
    /// its one name (CL68.3, ADR 0133).
    /// </summary>
    [Test]
    public void ALookupAgentsAddress_IsItsName()
    {
        var address = new AgentAddress("testprod", "lookup");

        Assert.Multiple(() =>
        {
            Assert.That(AgentAddresses.ResolveLookup(address, Identities, out _),
                Is.EqualTo(new AddressedAgent(address, AddressKind.Lookup, "testprod.lookup")));
            Assert.That(AgentAddresses.IsLookup(address), Is.True);
            Assert.That(AgentAddresses.IsLookup(new AgentAddress("testprod", "researcher")), Is.False);
        });
    }

    /// <summary>
    /// A lookup agent address with no such lookup agent is refused, naming the identity's lookup agents by their
    /// addresses.
    /// </summary>
    [Test]
    public void AnUnknownLookupAgent_IsRefused_NamingTheIdentitysLookupAgents()
    {
        var agent = AgentAddresses.ResolveLookup(new AgentAddress("testprod", "cook"), Identities, out string? why);

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
    /// A role with no file reaches no one, and the reason names the identity's roles by their addresses.
    /// </summary>
    [TestCase("cook")]
    [TestCase("helper")]
    public void AnUnknownRole_IsRefused_NamingTheIdentitysRoles(string role)
    {
        var (agent, why) = Resolve("testprod", role);

        Assert.That(agent, Is.Null);
        Assert.That(why, Does.Contain($"'{role}'").And.Contain("testprod.researcher").And.Not.Contain("page-lookup"));
    }

    /// <summary>A role file belongs to the identity it is named for: another identity can't reach it.</summary>
    [Test]
    public void AnotherIdentitysRole_IsRefused()
    {
        var (agent, why) = Resolve("other", "researcher");

        Assert.That(agent, Is.Null);
        Assert.That(why, Does.Contain("other").And.Contain("'researcher'"));
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
