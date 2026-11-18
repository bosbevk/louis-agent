using LouisAgent.Web.Components.Site.Docs;

namespace LouisAgent.SiteFramework.Components.Orbi;

/// <summary>
/// The site's ways to ask the framework Orbi (F164.2): its button in the header, and what a docs page offers to ask
/// about itself. The page the visitor reads goes with the question, so Orbi knows which page "this" is.
/// </summary>
public static class OrbiAsks
{
    /// <summary>The header's button, which opens Orbi with nothing asked.</summary>
    public const string Open = "Ask Orbi";

    /// <summary>"Ask about this page", over "On this page".</summary>
    public static readonly PageAsk ThisPage =
        new("Ask about this page",
            "Explain this page to me. What does it cover, and where does it fit in the framework?");
}
