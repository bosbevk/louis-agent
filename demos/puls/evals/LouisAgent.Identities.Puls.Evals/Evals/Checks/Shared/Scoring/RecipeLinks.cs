using System.Text.RegularExpressions;
using LouisAgent.Evals.Markdown;

namespace LouisAgent.Identities.Puls.Evals.Checks.Shared.Scoring;

/// <summary>
/// The images of a text that could be a photo of the dish: every image, without the share buttons, print and PDF
/// icons, logos and avatars that the recipe pages the cook reads carry.
/// </summary>
/// <remarks>
/// Puls's own part of the old <c>Links</c> (Extract Class, Fowler, F49.3 step 1a): finding the images is the check
/// contract's <see cref="Links.ImageUrls"/>; knowing a recipe page's furniture is Puls's.
/// </remarks>
public static partial class RecipeLinks
{
    /// <summary>
    /// The words in an image's address that make it a share button, an icon or a logo: furniture of the page, never
    /// the dish.
    /// </summary>
    [GeneratedRegex(
        @"(/plugins/|/icons?/|/emoji/|gravatar|sprite|\b(logo|icon|avatar|badge|button|buttons|share|print|pdf|facebook|twitter|pinterest|linkedin|whatsapp|instagram|youtube|email)\b|[-_./](logo|icon|avatar|badge|button|share|print|pdf|facebook|twitter|pinterest|linkedin|whatsapp)[-_.0-9]*\.(png|jpe?g|gif|svg|webp))",
        RegexOptions.IgnoreCase)]
    private static partial Regex PageFurniture();

    /// <summary>
    /// The images of a text that could be a photo of the dish: <see cref="Links.ImageUrls"/> without the share
    /// buttons, print and PDF icons, logos and avatars that the pages the cook reads carry (a recipe page's own plugin
    /// images are not its recipe's photo).
    /// </summary>
    public static IReadOnlyList<string> RecipePhotos(string? text) =>
        Links.ImageUrls(text).Where(url => !PageFurniture().IsMatch(url)).ToList();
}
