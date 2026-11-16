using LouisAgent.Web.Components.Site.Text;

namespace LouisAgent.Site.Components.Architecture.Framework;

/// <summary>The framework's own site on the Architecture page: this site and what it is made of.</summary>
public partial class SiteFamily
{
    /// <summary>Each project of the site, and what it is.</summary>
    private static readonly IReadOnlyList<Term> Terms =
    [
        new(["louis-agent.site"], "This site: its pages, its graphs and its words, configured on the framework's controls."),
        new(["louis-agent.identity.guide"], "The guide in the corner: who it is, as markdown, and the docs it answers from."),
        new(["louis-agent.identity.showroom"], "Three made-up businesses' agents, recorded once for the demo pages."),
        new(["louis-agent.site-api"], "The site's host, a few lines: the site's pages as HTML, and the guide with its three packs, nothing else.")
    ];
}
