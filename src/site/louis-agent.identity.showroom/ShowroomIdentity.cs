using LouisAgent.Choices;
using LouisAgent.Composition.Packs;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Identities.Showroom;

/// <summary>
/// A made-up business's agent for the site's demos (F128.2): its name, its voice under that name, and the leads pack,
/// which passes a booking on to the owner.
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

    /// <inheritdoc/>
    public IFileProvider Prompts { get; } =
        new ManifestEmbeddedFileProvider(typeof(ShowroomIdentity).Assembly, "Prompts");

    /// <inheritdoc/>
    public IReadOnlyList<string> Packs { get; } =
        Enum.GetValues<ShowroomPack>().Select(pack => pack.Wire()).ToList();
}
