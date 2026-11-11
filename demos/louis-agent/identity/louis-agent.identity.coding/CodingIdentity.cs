using LouisAgent.Choices;
using LouisAgent.Composition.Packs;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Identities.Coding;

/// <summary>
/// Who the coding agent is: how it talks and routes work, and the packs it uses. Its tools are all packs.
/// </summary>
/// <remarks>
/// An identity, like Puls: the framework ships no agent of its own, so a demo says who runs on it (ADR 0027, ADR 0074).
/// </remarks>
public sealed class CodingIdentity : IAgentIdentity
{
    /// <inheritdoc/>
    public string Name => CodingIdentityName.LouisAgent.Wire();

    /// <inheritdoc/>
    public string DefaultVoice => $"{Name}/voice";

    /// <inheritdoc/>
    public IFileProvider Prompts { get; } = EmbeddedPrompts.Of(typeof(CodingIdentity).Assembly);

    /// <inheritdoc/>
    public IReadOnlyList<string> Packs { get; } = Enum.GetValues<CodingPack>().Select(pack => pack.Wire()).ToList();
}

/// <summary>This identity's name (ADR 0043).</summary>
public enum CodingIdentityName
{
    [WireName("louis-agent")] LouisAgent,
}

/// <summary>
/// The packs the coding agent names (ADR 0043: an identity keeps its own enum and never references a pack).
/// </summary>
public enum CodingPack
{
    Files,
    Git,
    DotNet,
    Shell,
    Web,
}
