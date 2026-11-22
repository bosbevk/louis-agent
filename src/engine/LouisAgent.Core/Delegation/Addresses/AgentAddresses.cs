using LouisAgent.Composition.Agents;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Packs;
using LouisAgent.Core.Delegation.Messages;
using LouisAgent.Core.Delegation.Messages.Holes;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Roles;

namespace LouisAgent.Core.Delegation.Addresses;

/// <summary>
/// Finds who an address reaches on this instance: the identity's voice, or one of its subagents, named by the address
/// itself (<c>acme.cook</c>, ADR 0133), or for the minion tool a minion's file
/// (<c>minions/{identity}-minion-{role}.md</c>, F127.5), or says why it reaches no one.
/// </summary>
/// <remarks>
/// It takes the identities this instance runs, not the catalogue: delegation sits below the agent in core's layers
/// (ADR 0010), and the identities are composition's, which every layer may read.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0117-one-address-for-every-agent-call.md">ADR 0117: one address for every agent call</seealso>
public static class AgentAddresses
{
    /// <summary>The agent the address reaches here, or the reason the model reads instead.</summary>
    /// <remarks>Guard clauses (Fowler), one per way it can fail, then the role's file.</remarks>
    public static AddressedAgent? Resolve(AgentAddress address, IReadOnlyList<IAgentIdentity> identities,
        out string? why)
    {
        var identity = identities.FirstOrDefault(i =>
            string.Equals(i.Name, address.Identity, StringComparison.OrdinalIgnoreCase));
        if (identity is null)
            return Refuse(SubSessionToolMessage.AddressUnknownIdentity, UnknownIdentity(address, identities), out why);
        if (address.Role is null && identity.DefaultVoice is { } voice)
            return Found(new AddressedAgent(address, AddressKind.Identity, voice), out why);
        if (address.Role is null)
        {
            var noVoice = new AddressNoVoiceHoles(address.Identity, RolesOf(identity));
            return Refuse(SubSessionToolMessage.AddressNoVoice, noVoice, out why);
        }

        // A subagent's name is its address, on the identity's own name: acme.cook (ADR 0133).
        string subagent = $"{identity.Name}.{address.Role}";
        if (KindOf(subagent) is { } kind) return Found(new AddressedAgent(address, kind, subagent), out why);
        var unknownRole = new AddressUnknownRoleHoles(address.Identity, address.Role, RolesOf(identity));
        return Refuse(SubSessionToolMessage.AddressUnknownRole, unknownRole, out why);
    }

    /// <summary>
    /// An address on another server (F127.3.2): the server says whether it has the identity (its identities are read
    /// before a session is opened there), so here it is only named: its voice that server's to pick, a subagent by its
    /// address.
    /// </summary>
    public static AddressedAgent OnAnotherServer(AgentAddress address) => address.Role is null
        ? new AddressedAgent(address, AddressKind.Identity, null)
        : new AddressedAgent(address, AddressKind.Subagent, $"{address.Identity}.{address.Role}");

    /// <summary>
    /// The minion an address reaches (F127.5): <c>acme.lookup</c> is the file <c>minions/acme-minion-lookup.md</c>, its
    /// function after the identity's name; or the reason, naming the identity's minions by their addresses.
    /// </summary>
    /// <remarks>
    /// The minion tool says the address is a minion's, so the same words never reach a specialist and a minion both
    /// (Claude's pick Q1, of three: the file's name as the role, which leaks its prefix; a grammar of its own).
    /// </remarks>
    public static AddressedAgent? ResolveMinion(AgentAddress address, IReadOnlyList<IAgentIdentity> identities,
        out string? why)
    {
        var identity = identities.FirstOrDefault(i =>
            string.Equals(i.Name, address.Identity, StringComparison.OrdinalIgnoreCase));
        if (identity is null)
            return Refuse(SubSessionToolMessage.AddressUnknownIdentity, UnknownIdentity(address, identities), out why);
        if (IsMinion(address))
            return Found(new AddressedAgent(address, AddressKind.Minion, MinionFile(address)), out why);
        var unknownMinion = new AddressUnknownMinionHoles(address.Identity, address.Role ?? "", MinionsOf(identity));
        return Refuse(SubSessionToolMessage.AddressUnknownMinion, unknownMinion, out why);
    }

    /// <summary>The identity an address named that isn't here, with the ones that are, in order.</summary>
    private static AddressUnknownIdentityHoles UnknownIdentity(AgentAddress address,
        IReadOnlyList<IAgentIdentity> identities) =>
        new(address.Identity, string.Join(", ", identities.Select(i => i.Name).Order(StringComparer.Ordinal)));

    /// <summary>
    /// How a chain of asks names an agent (F127.5): a voice by its identity's name, since an identity asked in any of
    /// its languages is the same agent; any other role by its file.
    /// </summary>
    public static string? ChainKeyOf(string? role, string? identity) =>
        role is null || RoleCatalog.LevelOf(role) == RoleLevel.Agent ? identity ?? role : role;

    /// <summary>The same, for the agent an address reaches here.</summary>
    public static string ChainKeyOf(AddressedAgent agent) =>
        agent.Kind == AddressKind.Identity ? agent.Address.Identity : agent.Role ?? agent.Address.Identity;

    /// <summary>Whether the address names one of this instance's minions.</summary>
    public static bool IsMinion(AgentAddress address) =>
        address is { Role: not null, Peer: null } &&
        RoleCatalog.Available(RoleLevel.Minion).Contains(MinionFile(address));

    /// <summary>A minion's file: its identity, <c>minion</c>, and its function.</summary>
    private static string MinionFile(AgentAddress address) => $"{address.Identity}-minion-{address.Role}";

    /// <summary>One of this instance's subagents, by its address; a minion's is no role to ask (F127.5).</summary>
    private static AddressKind? KindOf(string subagent) =>
        RoleCatalog.Available(RoleLevel.Subagent).Contains(subagent) ? AddressKind.Subagent : null;

    /// <summary>The identity's subagents as the model writes them, <c>acme.cook</c>, or "(none)".</summary>
    private static string RolesOf(IAgentIdentity identity) =>
        Addresses(RoleLevel.Subagent, $"{identity.Name}.", identity.Name);

    /// <summary>The identity's minions as the model writes them, <c>acme.lookup</c>, or "(none)".</summary>
    private static string MinionsOf(IAgentIdentity identity) =>
        Addresses(RoleLevel.Minion, $"{identity.Name}-minion-", identity.Name);

    /// <summary>The files of a level that start with a prefix, as addresses on the identity, or "(none)".</summary>
    private static string Addresses(RoleLevel level, string prefix, string identity)
    {
        var roles = RoleCatalog.Available(level)
            .Where(file => file.StartsWith(prefix, StringComparison.Ordinal))
            .Select(file => $"{identity}.{file[prefix.Length..]}")
            .Order(StringComparer.Ordinal)
            .ToList();
        return roles.Count == 0 ? "(none)" : string.Join(", ", roles);
    }

    /// <summary>The agent found, with no reason.</summary>
    private static AddressedAgent Found(AddressedAgent agent, out string? why)
    {
        why = null;
        return agent;
    }

    /// <summary>No agent, with the reason the model reads.</summary>
    private static AddressedAgent? Refuse(SubSessionToolMessage message, object values, out string? why)
    {
        why = MessageText.Format(message, values);
        return null;
    }
}
