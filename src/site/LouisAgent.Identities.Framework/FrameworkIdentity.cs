using LouisAgent.Choices;
using LouisAgent.Composition.Packs;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Identities.Framework;

/// <summary>
/// Who the framework Orbi is (F164.2, Louis: "the agents name is orbi but the framework orbi", "so we have public and
/// framework"): the agent on the framework's own site, which answers a developer about the framework from its docs,
/// with a doc link, and shows the place it talks about. One voice, two packs: the docs and the site, and not the
/// leads, since its site doesn't sell.
/// </summary>
/// <remarks>
/// An identity of its own (F164's pick, "no we give him his own identity"), beside the public Orbi's, so each keeps
/// its own voice and skills and neither prompt carries the other's: an identity references no other identity
/// (ADR 0027).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0027-engine-identities-and-packs.md">ADR 0027: engine, identities and packs</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0113-the-guide-is-louis-and-says-it-is-the-ai.md">ADR 0113: the guide says it is the AI</seealso>
public sealed class FrameworkIdentity : IAgentIdentity
{
    /// <inheritdoc/>
    public string Name => FrameworkIdentityName.LouisAgentFramework.Wire();

    /// <inheritdoc/>
    public string DefaultVoice => $"{Name}/voice";

    /// <inheritdoc/>
    public IFileProvider Prompts { get; } = EmbeddedPrompts.Of(typeof(FrameworkIdentity).Assembly);

    /// <inheritdoc/>
    public IReadOnlyList<string> Packs { get; } =
        Enum.GetValues<FrameworkPack>().Select(pack => pack.Wire()).ToList();

    /// <summary>
    /// Every answer ends with choices, the site pack's Offer, which the engine asks for when an answer ends without
    /// it, as the public Orbi's do.
    /// </summary>
    public string? EndsEachAnswerWith => "Offer";

    /// <summary>
    /// Except after "Stop here", a walkthrough's last choice: then it says goodbye and offers nothing.
    /// </summary>
    public IReadOnlyList<string> StopsOn { get; } = ["Stop here"];
}

/// <summary>This identity's name (ADR 0043).</summary>
public enum FrameworkIdentityName
{
    [WireName("louis-agent-framework")] LouisAgentFramework,
}

/// <summary>
/// The packs the framework Orbi names (ADR 0043: an identity keeps its own enum and never references a pack): the
/// docs it answers from, and the site it shows places on.
/// </summary>
public enum FrameworkPack
{
    [WireName("docs")] Docs,
    [WireName("site")] Site,
}
