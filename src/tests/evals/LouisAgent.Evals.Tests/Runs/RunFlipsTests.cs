using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Transcripts;

namespace LouisAgent.Evals.Tests.Runs;

/// <summary>
/// F126.4: a person's flip, kept on the mark it corrects. Every sum of the run then counts the answer that counts,
/// the flipped one where there is a flip, and a run can say which of its conversations passed only after one.
/// </summary>
public class RunFlipsTests
{
    private static readonly DateTimeOffset Started = new(2026, 11, 23, 4, 30, 12, TimeSpan.Zero);
    private static readonly Flip Flipped = new(Started.AddHours(2), "It does say so, in other words.");

    /// <summary>With no flip a mark counts as the judge said; flipped, the other way, both ways round.</summary>
    [Test]
    public void AMark_CountsAsTheJudgeSaid_UntilItIsFlipped_ThenTheOtherWay()
    {
        var met = new Mark("c", true, "Why.");
        var missed = new Mark("c", false, "Why.");

        Assert.That((met.CountsAsMet(), missed.CountsAsMet()), Is.EqualTo((true, false)));
        Assert.That((met with { Flip = Flipped }).CountsAsMet(), Is.False);
        Assert.That((missed with { Flip = Flipped }).CountsAsMet(), Is.True);
    }

    /// <summary>A repeat whose one missed criterion is flipped to met passes, and its criteria count says so.</summary>
    [Test]
    public void ARepeat_WithItsOneMissedCriterionFlipped_Passes_AndCountsItMet()
    {
        var repeat = Repeat(1, Turn(missed: true));
        var flipped = Repeat(1, Turn(missed: true, flipped: true));

        Assert.That((repeat.Passed, flipped.Passed), Is.EqualTo((false, true)));
        Assert.That((flipped.CountCriteria().Met, flipped.CountCriteria().Of), Is.EqualTo((2, 2)));
        Assert.That(flipped.FindFirstMiss(), Is.Null);
    }

    /// <summary>A conversation that passed only after a flip says so; one that passed with none doesn't.</summary>
    [Test]
    public void AConversation_ThatPassedOnlyAfterAFlip_SaysSo_AndOneThatPassedWithoutDoesNot()
    {
        var afterAFlip = new ConversationRun("a", 1, [Repeat(1, Turn(missed: true, flipped: true))]);
        var asJudged = new ConversationRun("b", 1, [Repeat(1, Turn())]);
        var failed = new ConversationRun("c", 1, [Repeat(1, Turn(missed: true))]);

        Assert.That(afterAFlip.PassedAfterAFlip(), Is.True);
        Assert.That((asJudged.PassedAfterAFlip(), failed.PassedAfterAFlip()), Is.EqualTo((false, false)));
    }

    /// <summary>A repeat counts its flips, and a run adds every repeat's.</summary>
    [Test]
    public void ARepeatAndARun_CountTheirFlips()
    {
        var two = Repeat(1, Turn(missed: true, flipped: true), Turn(missed: true, flipped: true));
        var run = Run(new ConversationRun("a", 1, [two]), new ConversationRun("b", 1, [Repeat(1, Turn())]));

        Assert.That((two.CountFlips(), run.CountFlips()), Is.EqualTo((2, 2)));
        Assert.That(Repeat(1, Turn()).CountFlips(), Is.Zero);
    }

    /// <summary>The run's order: the failed, then those passed after a flip, then the rest, each in order.</summary>
    [Test]
    public void TheRunsOrder_IsTheFailed_ThenThosePassedAfterAFlip_ThenTheRest()
    {
        var run = Run(
            new ConversationRun("passed", 1, [Repeat(1, Turn())]),
            new ConversationRun("flipped", 1, [Repeat(1, Turn(missed: true, flipped: true))]),
            new ConversationRun("failed", 1, [Repeat(1, Turn(missed: true))]),
            new ConversationRun("passed-too", 1, [Repeat(1, Turn())]));

        Assert.That(run.OrderFailedFirst().Select(conversation => conversation.Id),
            Is.EqualTo(new[] { "failed", "flipped", "passed", "passed-too" }));
    }

    /// <summary>
    /// WithFlip flips one criterion of one turn of one repeat, counted from 1 as the pages show them, and leaves every
    /// other mark as it was; with no flip it puts it back.
    /// </summary>
    [Test]
    public void WithFlip_FlipsOneCriterion_AndLeavesEveryOtherMark_AndPutsItBackWithNone()
    {
        var run = Run(new ConversationRun("a", 2, [Repeat(1, Turn(), Turn(missed: true)), Repeat(2, Turn())]));

        var flipped = run.WithFlip("a", 1, 2, 2, Flipped)!;
        var marks = flipped.Conversations[0].Repeats.SelectMany(repeat => repeat.Turns)
            .SelectMany(turn => turn.Facts.Concat(turn.Criteria)).Select(mark => mark.Flip).ToList();

        Assert.That(flipped.Conversations[0].Repeats[0].Turns[1].Criteria[1].Flip, Is.EqualTo(Flipped));
        Assert.That(marks.Count(flip => flip is not null), Is.EqualTo(1));
        Assert.That(flipped.Passed, Is.EqualTo(1));
        Assert.That(flipped.WithFlip("a", 1, 2, 2, null)!.CountFlips(), Is.Zero);
    }

    /// <summary>A place the run doesn't have is none: a conversation, a repeat, a turn or a criterion.</summary>
    [TestCase("b", 1, 1, 1)]
    [TestCase("a", 3, 1, 1)]
    [TestCase("a", 1, 3, 1)]
    [TestCase("a", 1, 1, 3)]
    [TestCase("a", 1, 0, 1)]
    [TestCase("a", 1, 1, 0)]
    public void WithFlip_OnAPlaceTheRunDoesNotHave_IsNone(string conversation, int repeat, int turn, int criterion)
    {
        var run = Run(new ConversationRun("a", 2, [Repeat(1, Turn(), Turn()), Repeat(2, Turn())]));

        Assert.That(run.WithFlip(conversation, repeat, turn, criterion, Flipped), Is.Null);
    }

    /// <summary>A turn with one fact met and two criteria, the second missed if asked, and flipped if asked.</summary>
    private static TurnRun Turn(bool missed = false, bool flipped = false)
    {
        var transcript = new TurnTranscript("Ask?", "Answer.", [], TimeSpan.FromSeconds(3));
        var second = new Mark("Says it plainly", !missed, "Why.") { Flip = flipped ? Flipped : null };
        return new TurnRun(transcript, [new Mark("no-error", true, "Why.")],
            [new Mark("Says what it is", true, "Why."), second], null, null);
    }

    private static RepeatRun Repeat(int number, params TurnRun[] turns) => new(number, turns);

    private static EvalRun Run(params ConversationRun[] conversations) =>
        new("20261123-043012-guide", "guide", "louis-agent-guide", RunKind.Live, 1, Started, conversations);
}
