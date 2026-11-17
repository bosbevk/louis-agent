using LouisAgent.Choices;
using LouisAgent.Identities.Guide;
using LouisAgent.Site.Admin;
using LouisAgent.Site.Components.Showroom;
using LouisAgent.Site.Layout;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Areas.Focuses;
using LouisAgent.Web.Components.Widget.Bar.Opening;
using LouisAgent.Web.Themes.Flat;
using LouisAgent.Web.Themes.Glass;

namespace LouisAgent.Site;

/// <summary>
/// What louis-agent.dev is made of (F127.6): its showroom, with the site's own demos, the part of the site each
/// page is (F127.7), what its guide offers to be asked as the showroom's bar (F134.3), that bar opening under the
/// pointer (FX172), and its admin with the
/// overview counting the guide's leads (F127.8) and the site's own Resume area, where Louis edits his CV (F142),
/// named through the framework's builder; the guide in its corner runs on the chat's plumbing, which every site gets.
/// It offers the framework's two themes, glass first, so glass is what a first visit gets (F151.6).
/// </summary>
/// <remarks>
/// Composition Root (Seemann): the one place the site says which parts it uses. The program hands it to
/// <c>AddSiteAreas</c>, and the site's tests hand it the same, so they draw what the site draws.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public static class LouisAgentDevAreas
{
    /// <summary>
    /// Names the site's areas: the showroom, the rail and the covers drawn from its demos, and the part of the site
    /// each page is, which tells the guide how to lead (F127.7).
    /// </summary>
    public static void Name(SiteAreasBuilder areas) => areas.Showroom(SiteDemos.All).Focuses(Focuses)
        .Suggestions(GuideSuggestions).BarOpens(BarOpens.Hover)
        .Admin(typeof(AdminLayout)).Overview(count => count.Tool(LeadTool, "leads")).Add(ResumeArea.Area)
        .Glass().Flat();

    /// <summary>
    /// What the guide's bar offers to be asked, as chips, the first the one suggested, the last a way into the
    /// framework (FX173, Louis: "add a framework chip"). The same four stand on the showroom's start and on every
    /// demo's page, so the walk is of "this page", which is true on each of them.
    /// </summary>
    private static readonly string[] GuideSuggestions =
    [
        "Walk me through this page", "Which demo is like my business?", "What does it cost?",
        "Tell me about the framework"
    ];

    /// <summary>The tool the guide sends a lead with, which the admin's overview counts as leads (F127.8).</summary>
    private const string LeadTool = "SendLead";

    /// <summary>
    /// The guide sells on the ordinary pages, guides through the showroom and every demo, and is technical about the
    /// framework, the page that compares it with others included.
    /// </summary>
    private static void Focuses(SiteFocusesBuilder focuses)
    {
        focuses.Default(GuideFocus.Selling.Wire())
            .Under("/demos", GuideFocus.Demos.Wire())
            .Under("/framework", GuideFocus.Framework.Wire());
        foreach (var demo in SiteDemos.All.All) focuses.Under(demo.Address, GuideFocus.Demos.Wire());
    }
}
