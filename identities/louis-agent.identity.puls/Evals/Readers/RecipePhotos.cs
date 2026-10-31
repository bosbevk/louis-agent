using LouisAgent.Evals.Readers;
using LouisAgent.Identities.Puls.Evals.Checks.Shared.Scoring;

namespace LouisAgent.Identities.Puls.Evals.Readers;

/// <summary>
/// Reads the images that could be a photo of the dish: every <c>![alt](url)</c>, without the share buttons, print and
/// PDF icons, logos and avatars that the recipe pages the cook reads carry.
/// </summary>
/// <remarks>
/// An Adapter (Gang of Four, Louis's pick) over <see cref="RecipeLinks.RecipePhotos"/>, the cook's image rule's own
/// reading, so <c>image-shown</c> over this reader grades as <c>recipe-image</c> did. The page-furniture list is Puls's
/// knowledge of the sites it reads, so it stays in the identity, untouched, rather than move into the harness. The
/// eval file names it <c>recipe-photos</c>.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class RecipePhotos : IImageReader
{
    /// <inheritdoc />
    public string Name => "recipe-photos";

    /// <inheritdoc />
    public IReadOnlyList<string> Images(string text) => RecipeLinks.RecipePhotos(text);
}
