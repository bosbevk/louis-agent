using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Transcripts;

namespace LouisAgent.Evals.Tests.Runs;

/// <summary>
/// The pass rule, asked of the records: a conversation passes only when every repeat the run asked for was played
/// and passed, and a run counts the conversations that passed.
/// </summary>
public class RunRecordsTests
{
    private static readonly DateTimeOffset Started = new(2026, 11, 23, 4, 30, 12, TimeSpan.Zero);

    /// <summary>Three repeats asked for, all three passed.</summary>
    [Test]
    public void AConversation_WhoseThreeRepeatsAllPassed_HasPassed() =>
        Assert.That(new ConversationRun("guide-what-is", 3, [Repeat(1, true), Repeat(2, true), Repeat(3, true)])
            .Passed, Is.True);

    /// <summary>One of the three didn't, and the conversation hasn't passed.</summary>
    [Test]
    public void AConversation_WithOneRepeatThatDidNotPass_HasNotPassed() =>
        Assert.That(new ConversationRun("guide-what-is", 3, [Repeat(1, true), Repeat(2, false), Repeat(3, true)])
            .Passed, Is.False);

    /// <summary>A run that stopped part way holds fewer repeats than it asked for, and that is no pass.</summary>
    [Test]
    public void AConversation_HoldingFewerRepeatsThanAsked_HasNotPassed_EvenWhenEachOnePassed() =>
        Assert.That(new ConversationRun("guide-what-is", 3, [Repeat(1, true), Repeat(2, true)]).Passed, Is.False);

    /// <summary>The run's number is how many of its conversations passed.</summary>
    [Test]
    public void TheRunsPassed_IsTheCountOfItsConversationsThatPassed()
    {
        var run = new EvalRun("20261123-043012-guide", "guide", "louis-agent-guide", RunKind.Live, 1, Started,
        [
            new ConversationRun("guide-what-is", 1, [Repeat(1, true)]),
            new ConversationRun("guide-pricing", 1, [Repeat(1, false)]),
            new ConversationRun("guide-next", 1, [Repeat(1, true)])
        ]);

        Assert.That(run.Passed, Is.EqualTo(2));
    }

    /// <summary>F126.3.4: a repeat and a run add their turns' judge costs; none known stays none, never 0.</summary>
    [Test]
    public void ARepeatAndARun_AddTheirJudgeCosts_AndNoneKnownStaysNone()
    {
        var transcript = new TurnTranscript("Hi?", "Hello.", [], TimeSpan.FromSeconds(3));
        var judged = new TurnRun(transcript, [], [], null, null) { JudgeCost = 0.002m };
        var unknown = new TurnRun(transcript, [], [], null, null);
        var repeat = new RepeatRun(1, [judged, unknown, judged with { JudgeCost = 0.001m }]);
        var run = new EvalRun("20261123-043012-guide", "guide", "louis-agent-guide", RunKind.Live, 1, Started,
            [new ConversationRun("a", 1, [repeat]), new ConversationRun("b", 1, [new RepeatRun(1, [unknown])])]);

        Assert.That((repeat.SumJudgeCost(), run.SumJudgeCost()), Is.EqualTo(((decimal?)0.003m, (decimal?)0.003m)));
        Assert.That(new RepeatRun(1, [unknown]).SumJudgeCost(), Is.Null);
    }

    /// <summary>A repeat of one turn, with one fact met or not.</summary>
    private static RepeatRun Repeat(int number, bool met)
    {
        var transcript = new TurnTranscript("What is louis-agent?", "A framework.", [], TimeSpan.FromSeconds(3));
        var fact = new Mark("no-error", met, "Ended without an error.");
        return new RepeatRun(number, [new TurnRun(transcript, [fact], [], null, null)]);
    }
}
