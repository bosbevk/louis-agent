using LouisAgent.Api.Contracts.Admin.Reviews;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Reviews.Queue;

namespace LouisAgent.Puls.Tests.Admin.Reviews.Queue;

/// <summary>
/// The Review page's model (F53.3 step 2): the items the API handed over, which one is open, how many are graded
/// and how many are left; a grade or a skip opens the next, and past the last item nothing is open.
/// </summary>
public class ReviewQueueViewTests
{
    /// <summary>Two items of a queue of five: the API handed over the first two.</summary>
    private static readonly ReviewQueueReply Reply = new([Item("run_2", "bobotie"), Item("run_1", "sosaties")], 5);

    /// <summary>A fresh view opens the first item with nothing graded and the total left.</summary>
    [Test]
    public void AFreshView_OpensTheFirst_WithTheTotalLeft()
    {
        var view = ReviewQueueView.Of(Reply);

        Assert.That(view.Current?.Conversation.Task, Is.EqualTo("bobotie"));
        Assert.That((view.Graded, view.Left, view.Exhausted), Is.EqualTo((0, 5, false)));
    }

    /// <summary>A grade opens the next and counts; a skip opens the next and doesn't.</summary>
    [Test]
    public void AGrade_Counts_ASkip_DoesNot()
    {
        var graded = ReviewQueueView.Of(Reply).Next(graded: true);
        var skipped = ReviewQueueView.Of(Reply).Next(graded: false);

        Assert.That(graded.Current?.Conversation.Task, Is.EqualTo("sosaties"));
        Assert.That((graded.Graded, graded.Left), Is.EqualTo((1, 4)));
        Assert.That(skipped.Current?.Conversation.Task, Is.EqualTo("sosaties"));
        Assert.That((skipped.Graded, skipped.Left), Is.EqualTo((0, 5)));
    }

    /// <summary>Past the last item handed over, nothing is open and the view says it is exhausted.</summary>
    [Test]
    public void PastTheLastItem_NothingIsOpen()
    {
        var view = ReviewQueueView.Of(Reply).Next(graded: true).Next(graded: true);

        Assert.That(view.Current, Is.Null);
        Assert.That((view.Exhausted, view.Left), Is.EqualTo((true, 3)));
    }

    /// <summary>The words: the place in the queue, the progress, and nothing waiting, by name or for nobody.</summary>
    [Test]
    public void TheWords()
    {
        var view = ReviewQueueView.Of(Reply).Next(graded: true);

        Assert.That(ReviewQueueText.Place(view), Is.EqualTo("2 of 5"));
        Assert.That(ReviewQueueText.Progress(view), Is.EqualTo("1 graded · 4 left"));
        Assert.That(ReviewQueueText.Empty("Louis"), Is.EqualTo("Nothing waits for your grade, Louis."));
        Assert.That(ReviewQueueText.Empty(null), Is.EqualTo("Nothing waits for a grade."));
    }

    /// <summary>One waiting answer of a run, on this task.</summary>
    private static ReviewQueueItem Item(string run, string task) => new(run, "eval-beta",
        new DateTimeOffset(2026, 10, 27, 10, 0, 0, TimeSpan.Zero), [],
        new ConversationRow("mode-beta", 1, task, 0, null, 0, 1, [], "pass", []), ["Wat eet ons?"], "Iets.");
}
