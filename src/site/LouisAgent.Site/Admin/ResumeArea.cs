using LouisAgent.Web.Components.Areas;

namespace LouisAgent.Site.Admin;

/// <summary>
/// The Resume area of this site's admin (F142): the editor for Louis's CV, with its link in the sidebar and its card
/// on the admin's home. It is the site's own area, plugged into the framework's frame as the overview is (F144).
/// </summary>
/// <remarks>
/// The editor's page lives in the site's own project, which the router already reads, so the area names no pages of
/// its own. It registers nothing either: the page and the card use the API client every site has.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0120-nothing-is-hand-wired-from-the-framework.md">ADR 0120: nothing is hand-wired from the framework</seealso>
public static class ResumeArea
{
    /// <summary>The area, for the site to name with its others.</summary>
    public static SiteArea Area { get; } = new("resume", _ => { }, [],
        AdminLinks: typeof(ResumeLinks), AdminCard: typeof(ResumeCard));
}
