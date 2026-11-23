using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Runs.Answers;
using LouisAgent.Evals.Contracts.Transcripts;

namespace LouisAgent.Evals.Tests.Runs.Comparison;

/// <summary>
/// F126.4: a run compared with the run before it, asked of the run: how many passed before and now, which
/// conversations now pass and now fail, which criteria were added and taken out, how many marks were kept, and which
/// conversations were left out; and each criterion the run missed or a person flipped, named once.
/// </summary>
public class RunChangeTests
{
    private static readonly DateTimeOffset Started = new(2026, 11, 23, 13, 19, 0, TimeSpan.Zero);
    private static readonly Flip Flipped = new(Started.AddHours(1));

    /// <summary>The passed counts, and the conversations that now pass and now fail, each in the run's order.</summary>
    [Test]
    public void CompareWith_SaysHowManyPassed_AndWhichNowPassAndNowFail()
    {
        var before = Run(Conversation("guide-bank", met: false), Conversation("guide-pricing", met: true),
            Conversation("guide-what-is", met: true));
        var now = Run(Conversation("guide-bank", met: true), Conversation("guide-pricing", met: false),
            Conversation("guide-what-is", met: true));

        var change = now.CompareWith(before);

        Assert.That((change.PassedBefore, change.PassedNow), Is.EqualTo((2, 2)));
        Assert.That(change.NowPass, Is.EqualTo(new[] { "guide-bank" }));
        Assert.That(change.NowFail, Is.EqualTo(new[] { "guide-pricing" }));
    }

    /// <summary>
    /// A criterion the run has that the run before didn't on the same turn is added, one it lacks taken out; a mark
    /// the same as before is kept, counted over every repeat.
    /// </summary>
    [Test]
    public void CompareWith_NamesTheCriteriaAddedAndTakenOut_AndCountsTheMarksKept()
    {
        var before = Run(Conversation("guide-walk", met: true, criteria: ["Moves on", "Lights the area"]));
        var now = Run(Conversation("guide-walk", met: true, criteria: ["Moves on", "Names the area"]));

        var change = now.CompareWith(before);

        Assert.That(change.Criteria,
            Is.EqualTo(new[]
            {
                new CriterionChange("guide-walk", 1, "Names the area", true),
                new CriterionChange("guide-walk", 1, "Lights the area", false)
            }));
        Assert.That(change.CriteriaKept, Is.EqualTo(2), "Moves on, in each of two repeats");
    }

    /// <summary>A conversation the run before had and this run doesn't is left out.</summary>
    [Test]
    public void CompareWith_NamesTheConversationsLeftOut()
    {
        var before = Run(Conversation("guide-bank", met: true), Conversation("guide-what-is", met: true));
        var now = Run(Conversation("guide-what-is", met: true));

        Assert.That(now.CompareWith(before).LeftOut, Is.EqualTo(new[] { "guide-bank" }));
    }

    /// <summary>
    /// The run's tally names each criterion it missed or a person flipped, once, however many repeats did; a
    /// criterion met throughout and never flipped isn't named.
    /// </summary>
    [Test]
    public void TallyCriteria_NamesEachMissedOrFlippedCriterionOnce()
    {
        var run = Run(Conversation("guide-pricing", met: false), Conversation("guide-walk", met: false, flipped: true),
            Conversation("guide-what-is", met: true, criteria: ["Says what it is"]));

        var tally = run.TallyCriteria();

        Assert.That(tally,
            Is.EquivalentTo(new[]
            {
                new CriterionTally("guide-pricing: Says it plainly", 1, 0),
                new CriterionTally("guide-walk: Says it plainly", 0, 1)
            }));
    }

    /// <summary>A finished run of these conversations.</summary>
    private static EvalRun Run(params ConversationRun[] conversations) =>
        new("20261123-131900-guide", "guide", "louis-agent-guide", RunKind.Live, 2, Started, conversations)
        {
            Status = RunStatus.Finished,
            EndedAt = Started.AddMinutes(6)
        };

    /// <summary>
    /// A conversation of two repeats of one turn; its last criterion met or missed, flipped if asked. Its criteria
    /// are named by the conversation, so a tally tells two conversations' criteria apart.
    /// </summary>
    private static ConversationRun Conversation(string id, bool met, IReadOnlyList<string>? criteria = null,
        bool flipped = false)
    {
        var texts = criteria ?? [$"{id}: Says it plainly"];
        var transcript = new TurnTranscript("Hello?", "Hello.", [], TimeSpan.FromSeconds(3));
        var marks = texts.Select((text, at) => new Mark(text, at < texts.Count - 1 || met, "Why.")
        {
            Flip = flipped && at == texts.Count - 1 ? Flipped : null
        }).ToList();
        var turn = new TurnRun(transcript, [], marks, null, null);
        return new ConversationRun(id, 2, [new RepeatRun(1, [turn]), new RepeatRun(2, [turn])]);
    }
}
