using LouisAgent.Choices;
using LouisAgent.Composition.Packs;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Identities.Showroom;

/// <summary>
/// A made-up business's agent for the site's demos (F128.2): its name, its voice under that name, the leads pack,
/// which passes a booking on to the owner, and the site pack, which shows the visitor a place on its website (F127.1).
/// </summary>
/// <remarks>
/// A small base class, since the three businesses differ only in their names and their voices' words: each is one line
/// that says which it is, and the voice file says the rest.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0027-engine-identities-and-packs.md">ADR 0027: engine, identities and packs</seealso>
public abstract class ShowroomIdentity(ShowroomIdentityName name) : IAgentIdentity
{
    /// <inheritdoc/>
    public string Name => name.Wire();

    /// <inheritdoc/>
    public string DefaultVoice => $"{Name}/voice";

    /// <summary>The three businesses' voices, one provider over the project, which the catalogue reads once.</summary>
    private static readonly IFileProvider Shared = EmbeddedPrompts.Of(typeof(ShowroomIdentity).Assembly);

    /// <summary>
    /// The project's prompts, every business's voice at <c>agents/{business}/</c>: the three share one provider, so
    /// each business is named once (CL68.1) and the catalogue reads the provider once.
    /// </summary>
    public IFileProvider Prompts => Shared;

    /// <inheritdoc/>
    public IReadOnlyList<string> Packs { get; } =
        Enum.GetValues<ShowroomPack>().Select(pack => pack.Wire()).ToList();

    /// <summary>
    /// A made-up business, so it's a demonstration (F127.2): a booking tried on it reaches the people behind the
    /// showroom, marked a demo, and the visitor hears it was one.
    /// </summary>
    public bool IsDemonstration => true;
}
