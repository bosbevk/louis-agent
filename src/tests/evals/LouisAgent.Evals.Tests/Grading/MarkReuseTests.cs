using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Transcripts;
using LouisAgent.Evals.Grading;

namespace LouisAgent.Evals.Tests.Grading;

/// <summary>
/// F126.4: the one rule for when a mark carries over. Where a turn says the same as before and a criterion's words are
/// word for word the same, the earlier mark is the mark, with a person's flip and note.
/// </summary>
public class MarkReuseTests
{
    private static readonly Flip Flipped = new(new DateTimeOffset(2026, 11, 23, 15, 0, 0, TimeSpan.Zero), "It does.");

    private static readonly TurnTranscript Said = new("Next.", "Next up, packs.", [], TimeSpan.FromSeconds(9));

    /// <summary>The same turn and the same criterion give back the earlier mark, its flip and note with it.</summary>
    [Test]
    public void TheSameTurnAndCriterion_GiveBackTheEarlierMark_WithItsFlip()
    {
        var before = Turn(Said, flipped: true);

        var reused = MarkReuse.FindReusable(before, Said with { Took = TimeSpan.FromSeconds(2) }, "Lights the area");

        Assert.That(reused, Is.EqualTo(before.Criteria[0]));
        Assert.That(reused!.Flip, Is.EqualTo(Flipped));
    }

    /// <summary>A turn that says something else, or a criterion reworded, gives back none.</summary>
    [Test]
    public void AChangedTurnOrCriterion_GivesBackNone()
    {
        var before = Turn(Said, flipped: true);

        Assert.That(MarkReuse.FindReusable(before, Said with { Answer = "Packs." }, "Lights the area"), Is.Null);
        Assert.That(MarkReuse.FindReusable(before, Said, "Lights the area it talks about"), Is.Null);
    }

    /// <summary>
    /// The live run's flips are carried onto a played repeat where its turn says the same; a turn that answered
    /// otherwise keeps its own mark.
    /// </summary>
    [Test]
    public void CarryFlips_CopiesTheLiveRunsFlips_WhereTheAnswersAreTheSame()
    {
        var live = new RepeatRun(1, [Turn(Said, flipped: true), Turn(Said, flipped: true)]);
        var played = new RepeatRun(1, [Turn(Said), Turn(Said with { Answer = "Something else." })]);

        var carried = MarkReuse.CarryFlips(played, live);

        Assert.That(carried.Turns.Select(turn => turn.Criteria[0].Flip), Is.EqualTo(new[] { Flipped, null }));
        Assert.That(carried.Passed, Is.False);
        Assert.That(carried.CountFlips(), Is.EqualTo(1));
    }

    /// <summary>With no live run, the played repeat is as it was.</summary>
    [Test]
    public void CarryFlips_WithNoLiveRun_LeavesTheRepeatAsItWas()
    {
        var played = new RepeatRun(1, [Turn(Said)]);

        Assert.That(MarkReuse.CarryFlips(played, null), Is.SameAs(played));
    }

    /// <summary>A turn of this transcript with one criterion the judge said wasn't met, flipped if asked.</summary>
    private static TurnRun Turn(TurnTranscript said, bool flipped = false)
    {
        var criterion = new Mark("Lights the area", false, "It doesn't say so.") { Flip = flipped ? Flipped : null };
        return new TurnRun(said, [], [criterion], null, null);
    }
}
