namespace LouisAgent.Packs.Recipes.Library;

/// <summary>
/// One recipe page as the library keeps it: the print as the cook read it, with where it's from and when. It's kept for
/// good, because a recipe doesn't go stale. It holds nothing about who asked, and has no place to put that.
/// </summary>
/// <param name="Page">The site's own page, which stays the link and the credit.</param>
/// <param name="SiteSlug">The site in the table it came from.</param>
/// <param name="Title">The recipe's name, which is what a search looks at.</param>
/// <param name="Print">What <c>ReadRecipe</c> printed, inside the frame of untrusted data but without it.</param>
/// <param name="ReadAt">When it was read.</param>
/// <remarks>A record because it is a plain value (ADR 0040).</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record StoredRecipe(Uri Page, string SiteSlug, string Title, string Print, DateTimeOffset ReadAt);
