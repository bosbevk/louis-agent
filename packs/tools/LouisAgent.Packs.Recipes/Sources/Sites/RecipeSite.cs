namespace LouisAgent.Packs.Recipes.Sources.Sites;

/// <summary>
/// One recipe site we may read, as a row of the sites table: what to call it, where it lives, how its recipes are
/// read and searched, when we checked it and what its pages told us.
/// </summary>
/// <remarks>
/// A record because it is a plain value read from content (ADR 0040).
/// </remarks>
/// <param name="Slug">The short lower-case name <c>RECIPE_SOURCES</c> and the tools use.</param>
/// <param name="Name">The site as a person writes it.</param>
/// <param name="Home">Where the site lives; a search page and a host are worked out from it.</param>
/// <param name="ReadBy">What step 0 saw on one of its recipe pages; nothing acts on it yet.</param>
/// <param name="Search">How we search the site.</param>
/// <param name="Checked">The day its <c>robots.txt</c> and page signals were read.</param>
/// <param name="Signals">What those showed that is worth keeping, or null.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record RecipeSite(
    string Slug,
    string Name,
    Uri Home,
    RecipeReadBy ReadBy,
    RecipeSearch Search,
    DateOnly Checked,
    string? Signals)
{
    /// <summary>
    /// Whether a page is on this site: its host, with or without <c>www.</c>, and at or under its home. The search and
    /// the eval's check both ask here, so they can't drift apart on what counts as a site.
    /// </summary>
    /// <remarks>
    /// The home's path matters because Rooi Rose lives under <c>novanews.co.za/rooirose</c>: the publisher's other
    /// sections aren't Rooi Rose. The added slash keeps <c>/rooirose-extra</c> out.
    /// </remarks>
    public bool Covers(Uri page) =>
        string.Equals(HostOf(page), HostOf(Home), StringComparison.OrdinalIgnoreCase)
        && (page.AbsolutePath.TrimEnd('/') + "/").StartsWith(Home.AbsolutePath.TrimEnd('/') + "/",
            StringComparison.Ordinal);

    /// <summary>The address's host without a leading <c>www.</c>.</summary>
    public static string HostOf(Uri address) =>
        address.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? address.Host[4..] : address.Host;
}
