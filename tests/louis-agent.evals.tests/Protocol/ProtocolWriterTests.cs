using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Outcomes;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using LouisAgent.Evals.Protocol.Parts.Run;
using LouisAgent.Evals.Reports;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Tests.Protocol;

/// <summary>
/// The lines an eval writes are protocol version 2: each one is an event of the contract, read back here with the
/// contract's own reader, so the tests hold the records and never the JSON text.
/// </summary>
public class ProtocolWriterTests
{
    /// <summary>The fixed moment every line is stamped with.</summary>
    private static readonly DateTimeOffset At = new(2026, 10, 28, 9, 30, 15, 250, TimeSpan.Zero);

    /// <summary>A writer over a sink, with the clock standing still.</summary>
    private static (ProtocolWriter Writer, StringWriter Sink) NewWriter()
    {
        var sink = new StringWriter();
        return (new ProtocolWriter(sink, new FixedClock(At)), sink);
    }

    /// <summary>Everything the writer wrote, one line each.</summary>
    private static string[] Lines(StringWriter sink) =>
        sink.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>The one event the writer wrote, read with the contract's reader.</summary>
    private static T Single<T>(StringWriter sink) where T : EvalEvent =>
        (T)EvalProtocolJson.Read(Lines(sink).Single())!;

    /// <summary>A bit of JSON text as an element that outlives its document.</summary>
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    /// <summary>Every line is one event of version 2, with no marker in front and the clock's time on it.</summary>
    [Test]
    public void EveryLine_IsAnEventOfVersion2_StampedWithTheClock()
    {
        var (writer, sink) = NewWriter();

        writer.Begin("cook", 2, "abc123", [], []);
        writer.Mode(SampleMode.Alpha, 1, [SampleToolset.Alpha], null, null);
        writer.ModeDone(SampleMode.Alpha, 1);

        var events = Lines(sink).Select(EvalProtocolJson.Read).ToList();
        Assert.That(events, Has.None.Null);
        Assert.That(events.Select(e => e!.T), Has.All.EqualTo(At));
        Assert.That(Lines(sink), Has.None.StartsWith("@"));
    }

    /// <summary>Text goes out as it is: an accent stays an accent.</summary>
    [Test]
    public void AQuestion_KeepsItsAccents()
    {
        var (writer, sink) = NewWriter();

        writer.Question("cook", 1, "pannekoek", 1, "sess_1", "Gee my 'n resep – rand, wêreld");

        Assert.That(Lines(sink).Single(), Does.Contain("wêreld"));
        Assert.That(Single<QuestionEvent>(sink),
            Is.EqualTo(new QuestionEvent(At, "cook", 1, "pannekoek", 1, "sess_1",
                "Gee my 'n resep – rand, wêreld")));
    }

    /// <summary>
    /// The first line names the eval, its version, the commit, its split, its graders and its models.
    /// </summary>
    [Test]
    public void TheFirstLine_NamesTheEvalItRuns()
    {
        var (writer, sink) = NewWriter();

        writer.Begin("cook", 2, "abc123", [new FakeGrader("priced-with-source", 1, GraderKind.FixedRule)],
            [new ModelInfo("cook", ModelLevel.Agent, "puls-cook", "claude-haiku-5-5")]);

        var begin = Single<ProtocolEvent>(sink);
        Assert.That((begin.Version, begin.Eval, begin.EvalVersion, begin.Commit, begin.Split, begin.RescoreOf),
            Is.EqualTo((ProtocolEvent.CurrentVersion, "cook", 2, "abc123", RunSplit.Dev, (string?)null)));
        Assert.That(begin.Graders.Single(),
            Is.EqualTo(new GraderInfo("priced-with-source", 1, GraderKind.FixedRule, null)));
        Assert.That(begin.Models.Single(),
            Is.EqualTo(new ModelInfo("cook", ModelLevel.Agent, "puls-cook", "claude-haiku-5-5")));
        Assert.That(begin.Dataset, Is.Null);
    }

    /// <summary>
    /// The first line says which scenarios the run asked for, which run it re-scores and the dataset it graded
    /// against. Without the split a held-out run would be stored like a dev one, and would join the dev trend.
    /// </summary>
    [TestCase(RunSplit.Dev)]
    [TestCase(RunSplit.HeldOut)]
    [TestCase(RunSplit.All)]
    public void TheFirstLine_SaysItsSplit_ItsRescoreAndItsDataset(RunSplit split)
    {
        var (writer, sink) = NewWriter();
        var dataset = new DatasetManifest("cook", 6, "all the cook's questions", "3f9c0a1b2d4e", []);

        writer.Begin("cook", 2, "abc123", [], [], split, "run_old", dataset);

        var begin = Single<ProtocolEvent>(sink);
        Assert.That((begin.Split, begin.RescoreOf), Is.EqualTo((split, "run_old")));
        Assert.That(begin.Dataset, Is.EqualTo(new DatasetInfo("cook", 6, "3f9c0a1b2d4e")));
    }

    /// <summary>
    /// A mode's line carries its toolsets, its role, its model and how many conversations it will run.
    /// </summary>
    [Test]
    public void AModeLine_CarriesWhatItsSessionsOpenWith()
    {
        var (writer, sink) = NewWriter();

        writer.Mode(SampleMode.Alpha, 1, [SampleToolset.Alpha], "puls/voice-af", null, conversations: 2);

        Assert.That(Single<ModeEvent>(sink).Conversations, Is.EqualTo(2));
        Assert.That(Single<ModeEvent>(sink) with { Toolsets = [] },
            Is.EqualTo(new ModeEvent(At, SampleMode.Alpha, 1, [], "puls/voice-af", null, 2)));
        Assert.That(Single<ModeEvent>(sink).Toolsets, Is.EqualTo(new[] { SampleToolset.Alpha }));
    }

    /// <summary>A stream event keeps the session's JSON as it came.</summary>
    [Test]
    public void AStreamEvent_KeepsItsData()
    {
        var (writer, sink) = NewWriter();
        var data = Json("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hallo"}}""");

        writer.Sse("cook", 1, "pannekoek", 1, "content_block_delta", data);

        var sse = Single<SseEvent>(sink);
        Assert.That((sse.Key, sse.Type), Is.EqualTo((new TurnKey("cook", 1, "pannekoek", 1), "content_block_delta")));
        Assert.That(sse.Data.GetRawText(), Is.EqualTo(data.GetRawText()));
    }

    /// <summary>
    /// A turn carries its facts (who answered, the calls, the hosts, the error) and what ended it early.
    /// </summary>
    [Test]
    public void ATurn_CarriesItsFacts_AndItsFailure()
    {
        var (writer, sink) = NewWriter();
        var facts = new TurnFacts("puls-cook", "sub-1", 1, 3, ["www.checkers.co.za"], "the model said no");

        writer.Turn(SampleMode.Beta, 1, "bobotie", 2, "sess_1", "En vir 6?", "Hier is bobotie.", "puls-cook", 0.5,
            facts, TurnFailure.ApiError);

        var turn = Single<TurnEvent>(sink);
        Assert.That((turn.Session, turn.Question, turn.Answer, turn.Role, turn.Continued, turn.Seconds),
            Is.EqualTo(("sess_1", "En vir 6?", "Hier is bobotie.", "puls-cook", "sub-1", 0.5)));
        Assert.That((turn.AgentCalls, turn.SpecialistCalls, turn.Error, turn.Failure),
            Is.EqualTo((1, 3, "the model said no", (TurnFailure?)TurnFailure.ApiError)));
        Assert.That(turn.Hosts, Is.EqualTo(new[] { "www.checkers.co.za" }));
    }

    /// <summary>
    /// A conversation carries its grades (with what judged a model's), its outcome and its errors. The eval's own
    /// scores are no part of version 2, and nothing is measured yet, so every metric is unknown.
    /// </summary>
    [Test]
    public void AConversation_CarriesItsGradesOutcomeAndErrors_AndNoScores()
    {
        var (writer, sink) = NewWriter();
        Grade[] grades =
        [
            new Grade("meal-for-people", 3, 1, GradeOutcome.Pass, "feeds four",
                new GradeJudge("qwen3:8b", "abc123def456")),
            new Grade("priced-with-source", 1, 0.5, GradeOutcome.Fail, "1 of 2 priced with a source"),
        ];

        writer.Conversation("cook", 1, "pannekoek", "puls-cook", Json("""{"priced":2}"""), 3.25, grades,
            TrialOutcome.Fail, errors: 1);

        var conversation = Single<ConversationEvent>(sink);
        Assert.That((conversation.Role, conversation.Errors, conversation.Seconds, conversation.Outcome),
            Is.EqualTo(("puls-cook", 1, 3.25, TrialOutcome.Fail)));
        Assert.That(conversation.Grades, Is.EqualTo(new[]
        {
            new GradeInfo("meal-for-people", 3, 1, GradeOutcome.Pass, "feeds four", "qwen3:8b", "abc123def456"),
            new GradeInfo("priced-with-source", 1, 0.5, GradeOutcome.Fail, "1 of 2 priced with a source", null,
                null),
        }));
        Assert.That(conversation.Metrics, Is.EqualTo(new Metrics(null, null, null, null, null, null, null, null)));
        Assert.That(Lines(sink).Single(), Does.Not.Contain("\"scores\""));
    }

    /// <summary>A mode's repeat ends with its rates when it has them, and with none when it has none.</summary>
    [Test]
    public void AModeDone_CarriesItsRates_OrNone()
    {
        var (writer, sink) = NewWriter();

        writer.ModeDone("cook", 2, new PassShare(1, 0.5), k: 2, infrastructure: 1, retried: 1);
        writer.ModeDone("cook", 1);

        var done = Lines(sink).Select(line => (ModeDoneEvent)EvalProtocolJson.Read(line)!).ToList();
        Assert.That(done[0].Rates, Is.EqualTo(new RatesInfo(1, 0.5, 2, 1, 1)));
        Assert.That(done[1].Rates, Is.Null);
    }

    /// <summary>A slice of a mode's roll-up goes out as a summary event, its counts per grader and per kind.</summary>
    [Test]
    public void ASummary_IsTheSliceAsAnEvent()
    {
        var (writer, sink) = NewWriter();
        var line = new SummaryLine("cook", "budget", 6, 4, 2, 0, 0, ["cook-af"], false, 0.6666666666666666, 1, 0.5,
            [new GraderCount("within-budget", GraderKind.FixedRule, 4, 2, 0, 0.75)],
            [new KindCount(GraderKind.FixedRule, 4, 2, 0)]);

        writer.Summary(line);

        var summary = Single<SummaryEvent>(sink);
        Assert.That((summary.Mode, summary.Slice, summary.N, summary.Passed, summary.Failed, summary.TooFew),
            Is.EqualTo(("cook", "budget", 6, 4, 2, false)));
        Assert.That((summary.PassRate, summary.PassAtK, summary.PassHatK),
            Is.EqualTo(((double?)0.6666666666666666, (double?)1, (double?)0.5)));
        Assert.That(summary.Graders, Is.EqualTo(line.Graders));
        Assert.That(summary.Kinds, Is.EqualTo(line.Kinds));
        Assert.That(summary.Tasks, Is.EqualTo(new[] { "cook-af" }));
    }

    /// <summary>A stored line a re-score keeps is written exactly as it was.</summary>
    [Test]
    public void ACopiedLine_IsWrittenAsItWas()
    {
        var (writer, sink) = NewWriter();
        const string stored = """{"event":"mode-done","t":"2026-10-28T08:00:00Z","mode":"m","repeat":1,"rates":null}""";

        writer.Copy(stored);

        Assert.That(sink.ToString(), Is.EqualTo(stored + "\n"));
    }

    /// <summary>A grader that is only named here: the protocol's first line says what graders the eval has.</summary>
    private sealed class FakeGrader(string name, int version, GraderKind kind) : IGrader
    {
        /// <inheritdoc />
        public string Name { get; } = name;

        /// <inheritdoc />
        public int Version { get; } = version;

        /// <inheritdoc />
        public GraderKind Kind { get; } = kind;

        /// <inheritdoc />
        public bool Required => true;

        /// <inheritdoc />
        public IReadOnlySet<string> ExpectedKeys => System.Collections.Frozen.FrozenSet<string>.Empty;

        /// <summary>Never called: these tests only write the grader's name.</summary>
        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }
}
