using LouisAgent.Site.Pages.Framework;

namespace LouisAgent.Site.Tests.Pages.Framework;

/// <summary>
/// The Identities page (F109.4) lists what's really in the repo: every identity under a demo's <c>identity/</c>
/// folder has its place, so a new one can't be forgotten, the way the Packs page is held to the packs.
/// </summary>
public class IdentitiesPageTests
{
    /// <summary>
    /// Every identity project under <c>demos/*/identity/</c>, and the guide under <c>src/site/</c> (F124), is on the
    /// page.
    /// </summary>
    [Test]
    public void EveryIdentity_IsOnThePage()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<IdentitiesPage>();
        var identities = Directory.GetDirectories(Path.Combine(TestPaths.RepoRoot, "demos"))
            .Select(demo => Path.Combine(demo, "identity")).Where(Directory.Exists)
            .Append(Path.Combine(TestPaths.RepoRoot, "src", "site"))
            .SelectMany(folder => Directory.GetDirectories(folder, "louis-agent.identity.*"))
            .Select(Path.GetFileName).ToList();

        Assert.That(identities, Has.Count.GreaterThanOrEqualTo(3), "the search found the identities");
        Assert.That(identities.Where(identity => !cut.Markup.Contains(identity)), Is.Empty);
    }

    /// <summary>
    /// The page says what an identity is made of: the principles first, a voice, roles and the packs it names.
    /// </summary>
    [TestCase("principles")]
    [TestCase("voice")]
    [TestCase("role")]
    [TestCase("packs it names")]
    public void ThePage_SaysWhatAnIdentityHolds(string words)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<IdentitiesPage>();

        Assert.That(cut.Markup, Does.Contain(words));
    }
}
