using LouisAgent.CodeReview.Pages.Site;
using LouisAgent.CodeReview.Pages.Site.Framework;

namespace LouisAgent.CodeReview.Tests.Pages.Site.Framework;

/// <summary>The Packs page lists what's really in the repo, so a new pack can't be forgotten.</summary>
public class PacksPageTests
{
    /// <summary>Every pack under <c>packs/tools/</c> has its place on the page.</summary>
    [Test]
    public void EveryPack_IsOnThePage()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<PacksPage>();
        var packs = Directory.GetDirectories(Path.Combine(TestPaths.RepoRoot, "packs", "tools"), "louis-agent.pack.*")
            .Select(folder => Path.GetFileName(folder)!).ToList();

        Assert.That(packs, Has.Count.GreaterThanOrEqualTo(10), "the search found the packs");
        Assert.That(packs.Where(pack => !cut.Markup.Contains(pack)), Is.Empty);
    }
}
