using LouisAgent.Choices;
using LouisAgent.Composition.Packs;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Identities.Triage;

/// <summary>
/// Who the triage team is (F106.3.2): the manager is the voice, the hub every result passes through; the sorter, the
/// investigator and the reviewer are its roles, each with only the tools its job needs; the coder is the coding
/// identity, reached as a peer. One identity with roles, because the three only ever work for the manager.
/// </summary>
/// <remarks>
/// An identity beside the coding agent's and the expert's in one catalogue (ADR 0027, Louis's pick of one identity
/// with three roles): its prompts are files, its packs are names, and the workflow (F106.3.3) is the code between
/// them.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0027-engine-identities-and-packs.md">ADR 0027: engine, identities and packs</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0017-skills-by-level.md">ADR 0017: skills by level</seealso>
public sealed class TriageIdentity : IAgentIdentity
{
    /// <inheritdoc/>
    public string Name => TriageIdentityName.Triage.Wire();

    /// <inheritdoc/>
    public string DefaultVoice => $"{Name}/voice";

    /// <inheritdoc/>
    public IFileProvider Prompts { get; } =
        EmbeddedPrompts.Of(typeof(TriageIdentity).Assembly);

    /// <inheritdoc/>
    public IReadOnlyList<string> Packs { get; } =
        Enum.GetValues<TriageIdentityPack>().Select(pack => pack.Wire()).ToList();

    /// <summary>The manager's own three roles, which it asks by their addresses (F127.3.1).</summary>
    public IReadOnlyList<string> Calls { get; } = [.. Enum.GetValues<TriageCall>().Select(call => call.Wire())];
}

/// <summary>
/// The agents the manager may ask, by their addresses: its own roles, one each (ADR 0043: an identity keeps its own
/// enum of the names it uses).
/// </summary>
public enum TriageCall
{
    [WireName("triage.sorter")] Sorter,
    [WireName("triage.investigator")] Investigator,
    [WireName("triage.reviewer")] Reviewer,
}

/// <summary>This identity's name (ADR 0043).</summary>
public enum TriageIdentityName
{
    [WireName("triage")] Triage,
}
