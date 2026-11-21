using System.Diagnostics.CodeAnalysis;

namespace LouisAgent.Composition.Agents;

/// <summary>
/// Who answers a call to another agent: an identity (<c>acme</c>), or one of its roles (<c>acme.cook</c>), on this
/// instance or on the server it names before a colon (<c>partner:acme</c>, F127.3.2). It is read once from what the
/// model wrote, and carried as this from then on.
/// </summary>
/// <remarks>
/// Value Object (Fowler): two addresses with the same identity and role are the same address, and one that exists is a
/// valid one, so the code past the parse never checks the text again.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0117-one-address-for-every-agent-call.md">ADR 0117: one address for every agent call</seealso>
public sealed record AgentAddress(string Identity, string? Role, string? Peer = null)
{
    /// <summary>The longest a name in an address may be.</summary>
    public const int MaxNameLength = 64;

    /// <summary>
    /// Reads <c>{identity}</c> or <c>{identity}.{role}</c>, with its server before a colon when it names one, the case
    /// and the spaces around ignored, or says it isn't an address: nothing, more than one dot or colon, an empty part,
    /// or a character no name has.
    /// </summary>
    public static bool TryParse(string? text, [NotNullWhen(true)] out AgentAddress? address)
    {
        address = null;
        if (string.IsNullOrWhiteSpace(text)) return false;

        string[] servers = text.Trim().ToLowerInvariant().Split(':');
        if (servers.Length > 2 || (servers.Length == 2 && !IsName(servers[0]))) return false;
        string[] parts = servers[^1].Split('.');
        if (parts.Length > 2 || !parts.All(IsName)) return false;
        address = new AgentAddress(parts[0], parts.Length == 2 ? parts[1] : null,
            servers.Length == 2 ? servers[0] : null);
        return true;
    }

    /// <summary>The address as it is written: its server first when it names one.</summary>
    public override string ToString() =>
        (Peer is null ? "" : $"{Peer}:") + (Role is null ? Identity : $"{Identity}.{Role}");

    /// <summary>A name: a letter or digit first, then lowercase letters, digits and dashes.</summary>
    private static bool IsName(string part) =>
        part.Length is > 0 and <= MaxNameLength && char.IsAsciiLetterOrDigit(part[0]) &&
        part.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-');
}
