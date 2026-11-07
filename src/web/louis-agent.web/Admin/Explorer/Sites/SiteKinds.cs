namespace LouisAgent.Web.Admin.Explorer.Sites;

/// <summary>
/// The kinds of website a site's sources chart counts, the one that matters most first. A website goes under the first
/// kind it fits, and a site that names none sees all it read as one slice.
/// </summary>
/// <param name="Kinds">The kinds, in the order the chart draws them.</param>
public sealed record SiteKinds(IReadOnlyList<SiteKind> Kinds)
{
    /// <summary>For a site that doesn't sort the websites its agent reads.</summary>
    public static readonly SiteKinds None = new([]);
}
