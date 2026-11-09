using LouisAgent.Site.Pages;

namespace LouisAgent.Site.Tests.Pages;

/// <summary>
/// What's coming (F109.4) is drawn from the TODO: every feature the page names by its id has an open row there, so
/// the list can't outlive the plan, and each is marked planned, never shown as shipped.
/// </summary>
public class WhatsComingTests
{
    /// <summary>The ids of the TODO's open rows: the first cell of each row of its table.</summary>
    private static HashSet<string> OpenIds() =>
        File.ReadLines(Path.Combine(TestPaths.RepoRoot, "TODO.md"))
            .Where(line => line.StartsWith("| F") || line.StartsWith("| CL") || line.StartsWith("| FX"))
            .Select(line => line.Split('|')[1].Trim())
            .ToHashSet();

    /// <summary>Every id the page names is an open row of the TODO.</summary>
    [Test]
    public void EveryFeatureNamed_IsAnOpenRowOfTheTodo()
    {
        using var ctx = new BunitContext();
        var open = OpenIds();

        var cut = ctx.Render<WhatsComingPage>();
        var named = cut.FindAll(".plan-id").Select(id => id.TextContent.Trim()).ToList();

        Assert.That(open, Has.Count.GreaterThan(50), "the TODO was read");
        Assert.That(named, Has.Count.GreaterThanOrEqualTo(8), "the page names what's coming");
        Assert.That(named.Where(id => !open.Contains(id)), Is.Empty);
    }

    /// <summary>Each item is marked planned, and the page says when the list was written.</summary>
    [Test]
    public void EveryItem_IsMarkedPlanned_AndTheListIsDated()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<WhatsComingPage>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll(".plan-id"), Has.Count.EqualTo(cut.FindAll(".planned").Count));
            Assert.That(cut.Markup, Does.Contain("2026-11-08"));
        });
    }
}
