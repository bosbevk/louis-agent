namespace LouisAgent.Web.Tests.Rules.Sites;

/// <summary>
/// A link to a site makes a card wherever it is pasted (Facebook, LinkedIn, Slack, a chat), and the card is read from
/// the page's own head by a crawler that runs no script: so every site's <c>index.html</c> carries its title, its
/// description, its picture and its address as Open Graph tags (Louis, 2026-11-08, on a bare card: "this must be in
/// the framework"). The words and the picture are the site's own; the rule is the framework's.
/// </summary>
[TestFixture]
public class SiteLinkCardTests
{
    /// <summary>What a crawler reads for the card, each as an attribute it must find in the head.</summary>
    private static readonly string[] Tags =
    [
        "name=\"description\"", "property=\"og:title\"", "property=\"og:description\"", "property=\"og:image\"",
        "property=\"og:url\"", "name=\"twitter:card\""
    ];

    private static IEnumerable<TestCaseData> Sites() =>
        FrameworkWebSites.Folders().Select(folder =>
            new TestCaseData(folder).SetArgDisplayNames(FrameworkWebSites.NameOf(folder)));

    /// <summary>The page's head, up to where the body starts.</summary>
    private static string HeadOf(string site)
    {
        string page = File.ReadAllText(Path.Combine(site, "wwwroot", "index.html"));
        return page[..page.IndexOf("<body", StringComparison.Ordinal)];
    }

    /// <summary>Every tag a card is read from is in the head.</summary>
    [TestCaseSource(nameof(Sites))]
    public void ASitesPage_CarriesItsCard(string site)
    {
        string head = HeadOf(site);

        Assert.That(Tags.Where(tag => !head.Contains(tag)), Is.Empty, "a card needs every one of these");
    }

    /// <summary>
    /// A crawler fetches the picture and the address as given, so both are absolute, on HTTPS; a relative path
    /// shows no picture at all.
    /// </summary>
    [TestCaseSource(nameof(Sites))]
    public void ASitesPictureAndAddress_AreAbsolute(string site)
    {
        string head = HeadOf(site);

        Assert.That(ContentOf(head, "property=\"og:image\""), Does.StartWith("https://"));
        Assert.That(ContentOf(head, "property=\"og:url\""), Does.StartWith("https://"));
    }

    /// <summary>The content of the meta tag with this attribute, or empty when the tag is missing.</summary>
    private static string ContentOf(string head, string attribute)
    {
        int at = head.IndexOf(attribute, StringComparison.Ordinal);
        if (at < 0) return "";

        int tagEnd = head.IndexOf('>', at);
        int tagStart = head.LastIndexOf('<', at);
        string tag = head[tagStart..tagEnd];
        int content = tag.IndexOf("content=\"", StringComparison.Ordinal);
        if (content < 0) return "";

        int valueStart = content + "content=\"".Length;
        return tag[valueStart..tag.IndexOf('"', valueStart)];
    }
}
