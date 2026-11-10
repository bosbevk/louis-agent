namespace LouisAgent.Web.Components.Site.Showroom;

/// <summary>
/// A site's demos, in the order it lists them (F127.1). The showroom draws its rail, its phone chips and its start
/// screen's covers from them, so a new demo is one line here and one page.
/// </summary>
/// <remarks>
/// Registered once in a site's <c>Program.cs</c>, as <c>SiteFrames</c> is: the framework draws the showroom and the
/// site says what's in it, so the framework names no site (ADR 0084).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public sealed record ShowroomDemos(IReadOnlyList<ShowroomDemo> All)
{
    /// <summary>
    /// The demos by what a visitor comes for, the groups in their order, each group's demos in the site's.
    /// </summary>
    public IEnumerable<IGrouping<DemoGroup, ShowroomDemo>> Groups =>
        All.GroupBy(demo => demo.Group).OrderBy(group => group.Key);

    /// <summary>The demo at a page's address, whatever its case or a trailing slash; none on any other page.</summary>
    public ShowroomDemo? At(string path) =>
        All.FirstOrDefault(demo => string.Equals(demo.Address.TrimEnd('/'), path.TrimEnd('/'),
            StringComparison.OrdinalIgnoreCase));
}
