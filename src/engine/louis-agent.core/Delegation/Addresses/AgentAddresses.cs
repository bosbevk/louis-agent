using LouisAgent.Composition.Agents;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Packs;
using LouisAgent.Core.Delegation.Messages;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Roles;

namespace LouisAgent.Core.Delegation.Addresses;

/// <summary>
/// Finds who an address reaches on this instance: the identity's voice, or a role file named for it
/// (<c>specialists/{identity}-{role}.md</c>, <c>minions/{identity}-{role}.md</c>), or says why it reaches no one.
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

    private static AddressKind? KindOf(string file) => file switch
    {
        _ when RoleCatalog.Available(RoleLevel.Specialist).Contains(file) => AddressKind.Specialist,
        _ when RoleCatalog.Available(RoleLevel.Minion).Contains(file) => AddressKind.Minion,
        _ => null,
    };

    /// <summary>The identity's roles as the model writes them, <c>acme.cook</c>, or "(none)".</summary>
    private static string RolesOf(IAgentIdentity identity)
    {
        string prefix = $"{identity.Name}-";
        var roles = RoleCatalog.Available(RoleLevel.Specialist).Concat(RoleCatalog.Available(RoleLevel.Minion))
            .Where(file => file.StartsWith(prefix, StringComparison.Ordinal))
            .Select(file => $"{identity.Name}.{file[prefix.Length..]}")
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
