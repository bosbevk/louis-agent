using System.Text.Json;
using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Runs.Answers;
using LouisAgent.Evals.Contracts.Runs.Json;
using LouisAgent.Evals.Contracts.Transcripts;

namespace LouisAgent.Evals.Tests.Runs;

/// <summary>
/// What a page asks of a run is answered by the run's records: the counts, the cost and the time, the first miss,
/// the order failed first and the walk through the failed ones. None of it is written into the run's file.
/// </summary>
public class RunAnswersTests
{
    private static readonly DateTimeOffset Started = new(2026, 11, 23, 4, 30, 12, TimeSpan.Zero);

    /// <summary>A repeat counts its facts and its criteria over all its turns, as met and of.</summary>
    [Test]
    public void ARepeat_CountsItsFactsAndCriteria_MetAndOf()
    {
        var repeat = Repeat(1, Turn(facts: [true, false], criteria: [true]),
            Turn(facts: [true], criteria: [false, true]));

        Assert.That((repeat.CountFacts(), repeat.CountCriteria()),
            Is.EqualTo((new MarkCount(2, 3), new MarkCount(2, 3))));
    }

    /// <summary>A repeat's cost is its turns' known costs added; with none priced it is null, never 0.</summary>
    [Test]
    public void ARepeatsCost_AddsTheKnownCosts_AndIsNullWhenNoneIsPriced()
    {
        var mixed = Repeat(1, Turn(cost: 0.002m), Turn(cost: null));
        var unpriced = Repeat(1, Turn(cost: null), Turn(cost: null));

        Assert.That(mixed.SumCost(), Is.EqualTo(0.002m));
        Assert.That(unpriced.SumCost(), Is.Null);
    }

    /// <summary>A repeat's time is its turns' times added.</summary>
    [Test]
    public void ARepeatsTime_IsItsTurnsTimesAdded() =>
        Assert.That(Repeat(1, Turn(seconds: 4), Turn(seconds: 7)).SumTime(), Is.EqualTo(TimeSpan.FromSeconds(11)));

    /// <summary>
    /// The first miss is the first mark not met, turns in order and a turn's facts before its criteria, with the
    /// turn's number from 1; a repeat that passed has none.
    /// </summary>
    [Test]
    public void ARepeatsFirstMiss_IsTheFirstMarkNotMet_FactsBeforeCriteria()
    {
        var criterionFirst = Repeat(1, Turn(facts: [true], criteria: [true]), Turn(facts: [true], criteria: [false]));
        var factInTheSameTurn = Repeat(1, Turn(facts: [true]), Turn(facts: [true, false], criteria: [false]));

        var miss = criterionFirst.FindFirstMiss();
        Assert.That((miss?.Turn, miss?.Kind, miss?.Mark.Text), Is.EqualTo((2, MarkKind.Criterion, "criterion 1")));
        var fact = factInTheSameTurn.FindFirstMiss();
        Assert.That((fact?.Turn, fact?.Kind, fact?.Mark.Text), Is.EqualTo((2, MarkKind.Fact, "fact 2")));
        Assert.That(Repeat(1, Turn(facts: [true], criteria: [true])).FindFirstMiss(), Is.Null);
    }

    /// <summary>A conversation counts its passed repeats and finds its first failed one, if any.</summary>
    [Test]
    public void AConversation_CountsItsPassedRepeats_AndFindsItsFirstFailedOne()
    {
        var conversation = new ConversationRun("guide-what-is", 3,
            [Repeat(1, Turn()), Repeat(2, Turn(facts: [false])), Repeat(3, Turn(facts: [false]))]);
        var allPassed = new ConversationRun("guide-what-is", 1, [Repeat(1, Turn())]);

        Assert.That(conversation.CountPassedRepeats(), Is.EqualTo(1));
        Assert.That(conversation.FindFirstFailedRepeat()?.Number, Is.EqualTo(2));
        Assert.That(allPassed.FindFirstFailedRepeat(), Is.Null);
    }

    /// <summary>
    /// A conversation opens the repeat it is asked for; asked for none, its first failed repeat, or its first; and a
    /// number it doesn't hold is none.
    /// </summary>
    [Test]
    public void AConversation_OpensTheRepeatAskedFor_ElseTheFirstFailed_ElseTheFirst()
    {
        var failed = new ConversationRun("a", 3,
            [Repeat(1, Turn()), Repeat(2, Turn(facts: [false])), Repeat(3, Turn())]);
        var passed = new ConversationRun("b", 2, [Repeat(1, Turn()), Repeat(2, Turn())]);

        Assert.That(failed.FindRepeat(3)?.Number, Is.EqualTo(3));
        Assert.That(failed.FindRepeat(null)?.Number, Is.EqualTo(2));
        Assert.That(passed.FindRepeat(null)?.Number, Is.EqualTo(1));
        Assert.That(passed.FindRepeat(7), Is.Null);
    }

    /// <summary>A conversation's first ask is its first repeat's first turn's, and none with no turn.</summary>
    [Test]
    public void AConversationsFirstAsk_IsItsFirstRepeatsFirstTurns_OrNone()
    {
        var conversation = new ConversationRun("guide-what-is", 1, [Repeat(1, Turn(ask: "What is it?"), Turn())]);
        var empty = new ConversationRun("guide-what-is", 1, []);

        Assert.That(conversation.FindFirstAsk(), Is.EqualTo("What is it?"));
        Assert.That(empty.FindFirstAsk(), Is.Null);
    }

    /// <summary>A run counts and adds over every repeat of every conversation.</summary>
    [Test]
    public void ARun_CountsAndAddsOverEveryRepeat()
    {
        var run = Run(
            new ConversationRun("a", 2,
                [Repeat(1, Turn(facts: [true, false], cost: 0.001m)), Repeat(2, Turn(criteria: [true], cost: null))]),
            new ConversationRun("b", 1, [Repeat(1, Turn(facts: [true], criteria: [false], cost: 0.002m))]));

        Assert.That((run.CountFacts(), run.CountCriteria()), Is.EqualTo((new MarkCount(3, 4), new MarkCount(1, 2))));
        Assert.That(run.SumCost(), Is.EqualTo(0.003m));
        Assert.That(Run(new ConversationRun("a", 1, [Repeat(1, Turn(cost: null))])).SumCost(), Is.Null);
    }

    /// <summary>F126.3.4: a run's time so far runs to now while it goes, and to its end once it ended.</summary>
    [Test]
    public void ARunsTimeSoFar_IsToNowWhileItGoes_AndToItsEndOnceEnded()
    {
        var now = Started.AddSeconds(158);

        Assert.That(Run().MeasureTimeSoFar(now), Is.EqualTo(TimeSpan.FromSeconds(158)));
        Assert.That((Run() with { EndedAt = Started.AddSeconds(30) }).MeasureTimeSoFar(now),
            Is.EqualTo(TimeSpan.FromSeconds(30)));
    }

    /// <summary>A run's currency is its first priced turn's, and none while no turn is priced.</summary>
    [Test]
    public void ARunsCurrency_IsItsFirstPricedTurns_OrNone()
    {
        var priced = Run(new ConversationRun("a", 2, [Repeat(1, Turn(cost: null)), Repeat(2, Turn(cost: 0.001m))]));

        Assert.That(priced.FindCurrency(), Is.EqualTo("USD"));
        Assert.That(Run(new ConversationRun("a", 1, [Repeat(1, Turn(cost: null))])).FindCurrency(), Is.Null);
    }

    /// <summary>A run's time is from its start to its end, and none while it has no end.</summary>
    [Test]
    public void ARunsTime_IsFromItsStartToItsEnd_AndNoneWhileItRuns()
    {
        var ended = Run() with { EndedAt = Started.AddSeconds(33) };

        Assert.That(ended.MeasureTime(), Is.EqualTo(TimeSpan.FromSeconds(33)));
        Assert.That(Run().MeasureTime(), Is.Null);
    }

    /// <summary>The conversations come failed first, and inside each group as the file has them.</summary>
    [Test]
    public void ARun_OrdersItsConversationsFailedFirst_KeepingTheFilesOrderInEachGroup() =>
        Assert.That(Walked().OrderFailedFirst().Select(c => c.Id), Is.EqualTo(new[] { "b", "d", "a", "c" }));

    /// <summary>The next failed one after an id, in that order, and none after the last.</summary>
    [Test]
    public void ARun_FindsTheNextFailedConversation_AndNoneAfterTheLast()
    {
        var run = Walked();

        Assert.That(run.FindNextFailed("b")?.Id, Is.EqualTo("d"));
        Assert.That(run.FindNextFailed("d"), Is.Null);
        Assert.That(run.FindNextFailed("a"), Is.Null);
    }

    /// <summary>The conversation before an id, in that order, and none before the first.</summary>
    [Test]
    public void ARun_FindsTheConversationBefore_AndNoneBeforeTheFirst()
    {
        var run = Walked();

        Assert.That(run.FindPrevious("a")?.Id, Is.EqualTo("d"));
        Assert.That(run.FindPrevious("b"), Is.Null);
    }

    /// <summary>
    /// A run's file holds only the fields declared: its JSON's names are exactly these, a turn's judge cost the one
    /// F126.3.4 added, and a mark's flip, with its time and note, and a re-grade's run, the ones F126.4 added.
    /// </summary>
    [Test]
    public void ARunsFile_HoldsNoNewField()
    {
        var call = new ToolCallTranscript("ReadDoc", "{}", "# Docs", Failed: true);
        var transcript = new TurnTranscript("Ask?", "Answer.", [call], TimeSpan.FromSeconds(3), "An error.");
        var flipped = new Mark("c", true, "Why.") { Flip = new Flip(Started, "Why not.") };
        var turn = new TurnRun(transcript, [new Mark("no-error", false, "Why.")], [flipped], 0.001m, "USD");
        var run = Run(new ConversationRun("a", 1, [new RepeatRun(1, [turn])])) with
        {
            Status = RunStatus.Finished,
            EndedAt = Started.AddSeconds(5),
            StoppedBecause = "Why.",
            ReplayOf = "20261123-043012-guide",
            PageSent = true
        };

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(run, RunJson.Options));
        var names = new SortedSet<string>(StringComparer.Ordinal);
        Collect(json.RootElement, names);

        Assert.That(names, Is.EqualTo(new SortedSet<string>(StringComparer.Ordinal)
        {
            "agent",
            "answer",
            "ask",
            "asked",
            "at",
            "calls",
            "conversations",
            "cost",
            "criteria",
            "currency",
            "endedAt",
            "error",
            "eval",
            "facts",
            "failed",
            "flip",
            "id",
            "input",
            "judgeCost",
            "kind",
            "met",
            "note",
            "number",
            "pageSent",
            "passed",
            "reason",
            "regradeOf",
            "repeats",
            "replayOf",
            "result",
            "startedAt",
            "status",
            "stoppedBecause",
            "text",
            "took",
            "tool",
            "transcript",
            "turns"
        }));
    }

    /// <summary>
    /// An eval's last run passed in full when it finished and every one of the eval's conversations passed; a run
    /// that stopped part way, one with a failure, or no run at all did not.
    /// </summary>
    [Test]
    public void AnEvalsLastRun_PassedInFull_OnlyWhenItFinishedWithEveryConversationPassed()
    {
        var finished = RunRow.Of(Run(new ConversationRun("a", 1, [Repeat(1, Turn())])) with
        {
            Status = RunStatus.Finished
        });

        Assert.That(new EvalRow("guide", "louis-agent-guide", 1, finished).LastRunPassedInFull(), Is.True);
        Assert.That(new EvalRow("guide", "louis-agent-guide", 2, finished).LastRunPassedInFull(), Is.False);
        Assert.That(new EvalRow("guide", "louis-agent-guide", 1, finished with { Status = RunStatus.CouldNotFinish })
            .LastRunPassedInFull(), Is.False);
        Assert.That(new EvalRow("guide", "louis-agent-guide", 1, null).LastRunPassedInFull(), Is.False);
    }

    /// <summary>An eval counts the conversations a run of it didn't reach, and never fewer than none.</summary>
    [Test]
    public void AnEval_CountsTheConversationsARunDidNotReach()
    {
        var run = Run(new ConversationRun("a", 1, [Repeat(1, Turn())]), new ConversationRun("b", 1, []));

        Assert.That(new EvalRow("guide", "louis-agent-guide", 4, null).CountNotRunIn(run), Is.EqualTo(2));
        Assert.That(new EvalRow("guide", "louis-agent-guide", 1, null).CountNotRunIn(run), Is.Zero);
    }

    /// <summary>F126.3.4: a run in short carries what it cost the agent and the judge, and in what currency.</summary>
    [Test]
    public void ARunInShort_CarriesItsTwoCosts_AndItsCurrency()
    {
        var turn = Turn(cost: 0.002m) with { JudgeCost = 0.001m };
        var row = RunRow.Of(Run(new ConversationRun("a", 1, [Repeat(1, turn, Turn(cost: 0.003m))])));

        Assert.That((row.Cost, row.JudgeCost, row.Currency), Is.EqualTo(((decimal?)0.005m, (decimal?)0.001m, "USD")));
    }

    /// <summary>
    /// F126.3.4: an eval's live estimate is what its last live run spent a repeat, the agent's and the judge's
    /// added, times the repeats asked.
    /// </summary>
    [TestCase(1, 0.02, 0.01, 3, 0.09)]
    [TestCase(3, 0.06, 0.03, 1, 0.03)]
    public void AnEvalsEstimate_IsWhatItsLastLiveRunSpentARepeat_TimesTheRepeats(int ran, decimal agent,
        decimal judge, int asked, decimal estimate)
    {
        var lastLive = LastLive(ran) with
        {
            Cost = agent,
            JudgeCost = judge
        };

        Assert.That(new EvalRow("guide", "louis-agent-guide", 1, null, lastLive).EstimateLive(asked),
            Is.EqualTo(estimate));
    }

    /// <summary>
    /// FX234: the estimate scales with the conversations too. The guide's eval grew from one conversation to twelve,
    /// and the estimate, from a one-conversation run, still said $0.012 for a run that cost $0.090.
    /// </summary>
    [Test]
    public void AnEvalsEstimate_ScalesWithItsConversations_AsWellAsItsRepeats()
    {
        // The last live run: one conversation, three repeats, $0.012 in all, so $0.004 a conversation played once.
        var lastLive = LastLive(3) with
        {
            Cost = 0.008m,
            JudgeCost = 0.004m
        };

        Assert.That(new EvalRow("guide", "louis-agent-guide", 12, null, lastLive).EstimateLive(3),
            Is.EqualTo(0.144m));
    }

    /// <summary>With no live run, or one that had no price, there is no estimate, never 0.</summary>
    [Test]
    public void WithNoLiveRun_OrNoPrice_ThereIsNoEstimate()
    {
        Assert.That(new EvalRow("guide", "louis-agent-guide", 1, null).EstimateLive(3), Is.Null);
        Assert.That(new EvalRow("guide", "louis-agent-guide", 1, null, LastLive(1)).EstimateLive(3), Is.Null);
    }

    /// <summary>An eval can be replayed only when it has a live run that finished.</summary>
    [Test]
    public void AnEval_CanReplay_OnlyWithALiveRun()
    {
        Assert.That(new EvalRow("guide", "louis-agent-guide", 1, null, LastLive(1)).CanReplay(), Is.True);
        Assert.That(new EvalRow("guide", "louis-agent-guide", 1, null).CanReplay(), Is.False);
    }

    /// <summary>A finished live run in short, of this many repeats, with no price.</summary>
    private static RunRow LastLive(int repeats) =>
        RunRow.Of(Run(new ConversationRun("a", repeats, [Repeat(1, Turn())])) with
        {
            Status = RunStatus.Finished,
            Repeats = repeats
        });

    /// <summary>Every property name in a JSON value, at any depth.</summary>
    private static void Collect(JsonElement element, SortedSet<string> names)
    {
        if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray())
                Collect(item, names);
        if (element.ValueKind != JsonValueKind.Object) return;

        foreach (var property in element.EnumerateObject())
        {
            names.Add(property.Name);
            Collect(property.Value, names);
        }
    }

    /// <summary>Four conversations, the second and the fourth failed.</summary>
    private static EvalRun Walked() => Run(
        new ConversationRun("a", 1, [Repeat(1, Turn())]),
        new ConversationRun("b", 1, [Repeat(1, Turn(facts: [false]))]),
        new ConversationRun("c", 1, [Repeat(1, Turn())]),
        new ConversationRun("d", 1, [Repeat(1, Turn(facts: [false]))]));

    /// <summary>A run of the guide's eval with these conversations, still running.</summary>
    private static EvalRun Run(params ConversationRun[] conversations) =>
        new("20261123-043012-guide", "guide", "louis-agent-guide", RunKind.Live, 1, Started, conversations);

    /// <summary>A repeat of these turns.</summary>
    private static RepeatRun Repeat(int number, params TurnRun[] turns) => new(number, turns);

    /// <summary>A turn with facts and criteria met or not, named in order, and its cost and time.</summary>
    private static TurnRun Turn(bool[]? facts = null, bool[]? criteria = null, decimal? cost = null,
        int seconds = 1, string ask = "Hi?")
    {
        var transcript = new TurnTranscript(ask, "Hello.", [], TimeSpan.FromSeconds(seconds));
        var factMarks = (facts ?? [true]).Select((met, at) => new Mark($"fact {at + 1}", met, "Why.")).ToList();
        var criterionMarks = (criteria ?? []).Select((met, at) => new Mark($"criterion {at + 1}", met, "Why."))
            .ToList();
        return new TurnRun(transcript, factMarks, criterionMarks, cost, cost is null ? null : "USD");
    }
}
