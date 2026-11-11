namespace LouisAgent.Core.Delegation.Addresses;

/// <summary>What an address reaches: an identity in its own voice, one of its specialists, or a minion role.</summary>
public enum AddressKind
{
    /// <summary>The identity itself, in its default voice (<c>agent/</c>).</summary>
    Identity,

    /// <summary>One of its specialists (<c>specialists/</c>).</summary>
    Specialist,

    /// <summary>One of its minion roles (<c>minions/</c>).</summary>
    Minion,
}
