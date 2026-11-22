namespace LouisAgent.Core.Delegation.Addresses;

/// <summary>
/// What an address reaches: an identity in its own voice, one of its subagents, or a lookup agent role.
/// </summary>
public enum AddressKind
{
    /// <summary>The identity itself, in its default voice (<c>agents/</c>).</summary>
    Identity,

    /// <summary>One of its subagents, inside its folder (<c>agents/{identity}/subagents/</c>, ADR 0133).</summary>
    Subagent,

    /// <summary>One of its lookup agent roles (<c>lookups/</c>).</summary>
    Lookup,
}
