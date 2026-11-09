using LouisAgent.Web.Components.Site.Meta.Schema;

namespace LouisAgent.CodeReview.Content;

/// <summary>
/// What the site tells a search engine about itself (F123): who runs it, the site, and the framework it is about. The
/// landing places each one, and the framework serves them in the page's head as structured data.
/// </summary>
/// <remarks>
/// The facts are the site's, so they live here and not in the framework, which only writes them; a crawler reads them
/// from the landing, where Google looks for who runs a site.
/// </remarks>
public static class SiteFacts
{
    private const string Name = "louis-agent";
    private const string Site = "https://louis-agent.dev/";

    /// <summary>Who runs the site: its name, its address, its icon and Louis's LinkedIn.</summary>
    public static SchemaOrganization Organization { get; } = new(Name, Site, $"{Site}apple-touch-icon.png",
        ["https://www.linkedin.com/in/louismarais86"]);

    /// <summary>The site itself.</summary>
    public static SchemaWebSite WebSite { get; } = new(Name, Site);

    /// <summary>The framework the site is about, and what it runs on.</summary>
    public static SchemaSoftwareApplication Software { get; } = new(Name,
        "An AI agent framework in C# on Microsoft.Extensions.AI: every turn streams as it happens, every request is "
        + "measured and priced, and an agent is a few files of your own.",
        "DeveloperApplication", ".NET 10 on Windows and Linux", Site);

    /// <summary>All three, as the landing's head carries them.</summary>
    public static IReadOnlyList<object> All { get; } = [Organization, WebSite, Software];
}
