using LouisAgent.Web.Components.Site.Docs;

namespace LouisAgent.SiteFramework.Components.Orbi;

/// <summary>
/// The site's ways to ask the framework Orbi (F164.2): its button in the header, what a docs page offers to ask about
/// itself, and the search, whose last row asks Orbi the visitor's words. The page the visitor reads goes with the
/// question, so Orbi knows which page "this" is.
/// </summary>
public static class OrbiAsks
{
    /// <summary>The header's button, which opens Orbi with nothing asked.</summary>
    public const string Open = "Ask Orbi";

    /// <summary>The header's search button.</summary>
    public const string Search = "Search";

    /// <summary>What the search's box says before the visitor types.</summary>
    public const string SearchPlaceholder = "Search the framework…";

    /// <summary>The search's last row, which asks Orbi the visitor's words; {0} their words.</summary>
    public const string SearchAsk = "Ask Orbi about \"{0}\"";

    /// <summary>"Ask about this page", over "On this page".</summary>
    public static readonly PageAsk ThisPage =
        new("Ask about this page",
            "Explain this page to me. What does it cover, and where does it fit in the framework?");
}
