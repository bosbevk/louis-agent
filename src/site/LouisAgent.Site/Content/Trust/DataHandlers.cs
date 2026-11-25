using LouisAgent.Web.Components.Site.Tables;

namespace LouisAgent.Site.Content.Trust;

/// <summary>
/// The companies that handle data for the site (F165), the subprocessors a business asks about: what each does for
/// us, what it sees, and where. It says only what the site's settings and the privacy page show.
/// </summary>
/// <remarks>
/// The table's rows as data, so the page only configures the framework's comparison table (ADR 0125).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public static class DataHandlers
{
    /// <summary>What is said of each company, across the top.</summary>
    public static readonly IReadOnlyList<string> Columns = ["What for", "What they see", "Where"];

    /// <summary>One row per company.</summary>
    public static readonly IReadOnlyList<ComparisonRow> Rows =
    [
        new("Microsoft Azure",
            ["Runs the site, keeps its keys and chats, and watches it",
                "Your chats, for 30 days after the last message, and traces without your words",
                "South Africa North"]),
        new("Anthropic", ["Its model writes the answers", "The messages of a chat", "United States"]),
        new("Resend", ["Sends us your details by email", "Your name and contact, after you press Send it",
            "Outside South Africa"]),
        new("Cloudflare", ["Points the domain at the site", "Only where a name points. Visits don't pass through it",
            "Worldwide"]),
        new("GitHub", ["Holds the code and releases it", "No visitor data", "United States"]),
    ];
}
