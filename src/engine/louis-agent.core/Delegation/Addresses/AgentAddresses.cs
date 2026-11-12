using LouisAgent.Composition.Agents;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Packs;
using LouisAgent.Core.Delegation.Messages;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Roles;

namespace LouisAgent.Core.Delegation.Addresses;

/// <summary>
/// Finds who an address reaches on this instance: the identity's voice, or a specialist's file named for it
/// (<c>specialists/{identity}-{role}.md</c>), or for the minion tool a minion's
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
            return Refuse(SubSessionToolMessage.AddressUnknownIdentity, new
            {
                address.Identity,
                Identities = string.Join(", ", identities.Select(i => i.Name).Order(StringComparer.Ordinal))
            }, out why);
        if (address.Role is null && identity.DefaultVoice is { } voice)
            return Found(new AddressedAgent(address, AddressKind.Identity, voice), out why);
        if (address.Role is null)
            return Refuse(SubSessionToolMessage.AddressNoVoice, new
            {
                address.Identity,
                Roles = RolesOf(identity)
            }, out why);

        // A role belongs to the identity its file is named for: acme.cook is specialists/acme-cook.md.
        string file = $"{identity.Name}-{address.Role}";
        if (KindOf(file) is { } kind) return Found(new AddressedAgent(address, kind, file), out why);
        return Refuse(SubSessionToolMessage.AddressUnknownRole, new
        {
            address.Identity,
            address.Role,
            Roles = RolesOf(identity)
        }, out why);
    }

    /// <summary>Which level holds a role file by this name, if any; a minions' base is no role.</summary>
    /// <summary>
    /// An address on another server (F127.3.2): the server says whether it has the identity (its identities are read
    /// before a session is opened there), so here it is only named: its voice that server's to pick, a role by the file
    /// name the convention gives it.
    /// </summary>
    public static AddressedAgent OnAnotherServer(AgentAddress address) => address.Role is null
        ? new AddressedAgent(address, AddressKind.Identity, null)
        : new AddressedAgent(address, AddressKind.Specialist, $"{address.Identity}-{address.Role}");

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
            return Refuse(SubSessionToolMessage.AddressUnknownIdentity, new
            {
                address.Identity,
                Identities = string.Join(", ", identities.Select(i => i.Name).Order(StringComparer.Ordinal))
            }, out why);
        if (IsMinion(address))
            return Found(new AddressedAgent(address, AddressKind.Minion, MinionFile(address)), out why);
        return Refuse(SubSessionToolMessage.AddressUnknownMinion, new
        {
            address.Identity,
            Role = address.Role ?? "",
            Minions = MinionsOf(identity)
        }, out why);
    }

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

    /// <summary>A specialist's file named for the identity; a minion's is no role to ask (F127.5).</summary>
    private static AddressKind? KindOf(string file) =>
        RoleCatalog.Available(RoleLevel.Specialist).Contains(file) ? AddressKind.Specialist : null;

    /// <summary>The identity's specialists as the model writes them, <c>acme.cook</c>, or "(none)".</summary>
    private static string RolesOf(IAgentIdentity identity) =>
        Addresses(RoleLevel.Specialist, $"{identity.Name}-", identity.Name);

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
