using LouisAgent.Choices;
using LouisAgent.Composition.Packs;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Identities.Guide;

/// <summary>
/// Who the framework's guide is (F109.3, renamed from the expert in F124): the guide in the site's corner, which
/// answers about the framework from its docs and nothing else. One voice, three packs.
/// </summary>
/// <remarks>
/// An identity of its own (Louis's pick A2), so a session names which identity it opens as, and the guide's voice and
/// packs stay its own files: no other agent's prompt carries them.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0027-engine-identities-and-packs.md">ADR 0027: engine, identities and packs</seealso>
public sealed class GuideIdentity : IAgentIdentity
{
    /// <inheritdoc/>
    public string Name => GuideIdentityName.LouisAgentGuide.Wire();

    /// <inheritdoc/>
    public string DefaultVoice => $"{Name}/voice";

    /// <inheritdoc/>
    public IFileProvider Prompts { get; } =
        EmbeddedPrompts.Of(typeof(GuideIdentity).Assembly);

    /// <inheritdoc/>
    public IReadOnlyList<string> Packs { get; } = Enum.GetValues<GuidePack>().Select(pack => pack.Wire()).ToList();

    /// <summary>
    /// The showroom's businesses, each in its own voice (F127.3.1), so the guide can show a visitor one.
    /// </summary>
    public IReadOnlyList<string> Calls { get; } = Enum.GetValues<GuideCall>().Select(call => call.Wire()).ToList();

    /// <summary>
    /// Every answer ends with choices (FX139, Louis: "the guide should always display options"): the site pack's
    /// Offer, which the engine asks for when an answer ends without it.
    /// </summary>
    public string? EndsEachAnswerWith => "Offer";
}

/// <summary>
/// The agents the guide may ask, by their addresses (ADR 0043: an identity keeps its own enum and never references
/// another identity).
/// </summary>
public enum GuideCall
{
    [WireName("seemeeu-guesthouse")] SeemeeuGuesthouse,
    [WireName("wheatfield-kitchen")] WheatfieldKitchen,
    [WireName("koringland-attorneys")] KoringlandAttorneys,
}

/// <summary>This identity's name (ADR 0043).</summary>
public enum GuideIdentityName
{
    [WireName("louis-agent-guide")] LouisAgentGuide,
}

/// <summary>The packs the guide names (ADR 0043: an identity keeps its own enum and never references a pack).</summary>
public enum GuidePack
{
    Docs,
    Leads,
    Site,
}
