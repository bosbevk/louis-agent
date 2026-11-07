using LouisAgent.Web.Admin.Explorer.Place;
using LouisAgent.Web.Admin.RunPage;

namespace LouisAgent.Web.Tests.Admin.Explorer.Place;

/// <summary>
/// Where you are in the explorer, as the address carries it (F53.1 step 3): the view and what is open in it written
/// as a query, read back from the query's parts, and the whole run address with its tab.
/// </summary>
public class ExplorerPlaceTests
{
    /// <summary>When a request was made; it names the request in the address.</summary>
    private static readonly DateTimeOffset At = new(2026, 10, 27, 10, 0, 2, 500, TimeSpan.Zero);

    /// <summary>The place writes only what its view needs, each value escaped for the address.</summary>
    [Test]
    public void ThePlace_WritesWhatItsViewNeeds()
    {
        Assert.That(ExplorerPlace.Start.Query(), Is.EqualTo("view=run"));
        Assert.That(new ExplorerPlace(ExplorerView.Conversations, Review: true).Query(),
            Is.EqualTo("view=conversations&review=true"));
        Assert.That(ExplorerPlace.Conversation("cook curry").Query(),
            Is.EqualTo("view=conversation&task=cook%20curry"));
        Assert.That(new ExplorerPlace(ExplorerView.Turn, "bobotie", 2).Query(),
            Is.EqualTo("view=turn&task=bobotie&turn=2"));
        Assert.That(new ExplorerPlace(ExplorerView.Request, "bobotie", 1, At, SampleMode.Alpha).Query(),
            Is.EqualTo("view=request&task=bobotie&turn=1&request=2026-10-27T10%3A00%3A02.5000000%2B00%3A00&mode=" +
                       SampleMode.Alpha));
    }

    /// <summary>The query's parts read back as the same place; a request's time comes back to the tick.</summary>
    [Test]
    public void TheQuerysParts_ReadBackAsTheSamePlace()
    {
        var place = new ExplorerPlace(ExplorerView.Request, "bobotie", 1, At, SampleMode.Alpha);

        var read = ExplorerPlace.Of("request", "bobotie", 1, "2026-10-27T10:00:02.5000000+00:00", SampleMode.Alpha,
            false);

        Assert.That(read, Is.EqualTo(place));
    }

    /// <summary>No view, or one we don't have, is the run; a request time that isn't one is no request.</summary>
    [Test]
    public void NoViewOrAnUnknownOne_IsTheRun_AndABadTimeIsNoRequest()
    {
        Assert.That(ExplorerPlace.Of(null, null, null, null, null, false), Is.EqualTo(ExplorerPlace.Start));
        Assert.That(ExplorerPlace.Of("kitchen", "bobotie", null, null, null, false).View, Is.EqualTo(ExplorerView.Run));
        Assert.That(ExplorerPlace.Of("request", "bobotie", 1, "yesterday", SampleMode.Alpha, false).RequestAt, Is.Null);
    }

    /// <summary>Each view is one level deeper than the one before, as the explorer counts them.</summary>
    [Test]
    public void EachView_IsOneLevelDeeper() =>
        Assert.That(Enum.GetValues<ExplorerView>().Select(v => new ExplorerPlace(v).Level),
            Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));

    /// <summary>The run's address is under admin/runs, with its tab and, on the conversations tab, the place.</summary>
    [Test]
    public void TheRunsAddress_HoldsItsTab_AndThePlace()
    {
        Assert.That(RunAddress.Of("run 1"), Is.EqualTo("admin/runs/run%201"));
        Assert.That(RunAddress.Of("run 1", RunTab.Notes, null), Is.EqualTo("admin/runs/run%201?tab=notes"));
        Assert.That(RunAddress.Of("run1", RunTab.Conversations, ExplorerPlace.Conversation("bobotie")),
            Is.EqualTo("admin/runs/run1?tab=conversations&view=conversation&task=bobotie"));
    }
}
