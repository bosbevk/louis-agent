namespace LouisAgent.Puls.Tests.Placeholder;

/// <summary>
/// Holds the public placeholder on mypuls.co.za to what F58 promises: a static page that names Puls and leads nowhere
/// that could spend money. Nothing in it may run code, send a form or link to the app's chat or admin, because the
/// page is the only Puls thing the internet can reach until accounts and rate limits exist (F43, F42).
/// </summary>
/// <remarks>The <i>static holding page</i> pattern, Louis's pick for F58; plain string checks, no regex (ADR 0057).</remarks>
public class PlaceholderPageTests
{
    private static readonly string Page = File.ReadAllText(
        Path.Combine(TestPaths.RepoRoot, "demos/puls/web/puls-placeholder/index.html"));

    /// <summary>A browser tab and a search result show Puls's name.</summary>
    [Test]
    public void ThePage_HasATitle() =>
        Assert.That(Page, Does.Contain("<title>Puls"));

    /// <summary>No script and no form: the page can't call anything or send anything.</summary>
    [TestCase("<script")]
    [TestCase("<form")]
    public void ThePage_RunsAndSendsNothing(string tag) =>
        Assert.That(Page, Does.Not.Contain(tag).IgnoreCase);

    /// <summary>No link into the app, so nobody is led to a page that isn't public.</summary>
    [TestCase("/chat")]
    [TestCase("/admin")]
    [TestCase("/v1/")]
    public void ThePage_LinksToNoPartOfTheApp(string path) =>
        Assert.That(Page, Does.Not.Contain(path).IgnoreCase);
}
