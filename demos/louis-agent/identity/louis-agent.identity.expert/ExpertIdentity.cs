using LouisAgent.Choices;
using LouisAgent.Composition.Packs;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Identities.Expert;

/// <summary>
/// Who the framework's guide is (F109.3): the expert in the site's corner, which answers about the framework from its
/// docs and nothing else. One voice, one pack.
/// </summary>
/// <remarks>
/// An identity beside the coding agent's, in the same catalogue (Louis's pick A2): a session names which of the two it
/// opens as, and the expert's voice and packs stay its own files, so the coding agent's prompt never carries them.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0027-engine-identities-and-packs.md">ADR 0027: engine, identities and packs</seealso>
public sealed class ExpertIdentity : IAgentIdentity
{
    /// <inheritdoc/>
    public string Name => ExpertIdentityName.LouisAgentExpert.Wire();

    /// <inheritdoc/>
    public string DefaultVoice => $"{Name}/voice";

    /// <inheritdoc/>
    public IFileProvider Prompts { get; } =
        new ManifestEmbeddedFileProvider(typeof(ExpertIdentity).Assembly, "Prompts");

    /// <inheritdoc/>
    public IReadOnlyList<string> Packs { get; } = Enum.GetValues<ExpertPack>().Select(pack => pack.Wire()).ToList();
}

/// <summary>This identity's name (ADR 0043).</summary>
public enum ExpertIdentityName
{
    [WireName("louis-agent-expert")] LouisAgentExpert,
}

/// <summary>The packs the expert names (ADR 0043: an identity keeps its own enum and never references a pack).</summary>
public enum ExpertPack
{
    Docs,
}
