using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using LouisAgent.Evals.Protocol.Parts.Run;
using LouisAgent.Evals.Running;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
using Microsoft.Extensions.Logging.Abstractions;

namespace LouisAgent.Evals.Tests.Running.Rescoring;

/// <summary>
/// A stored run written as protocol version 2 is re-scored as a version 1 run is: its first line gives the split and
/// the models, its turns are graded again, and its mode's end gives the retries it recorded.
/// </summary>
public class StoredRunV2Tests
{
    /// <summary>The time every line carries.</summary>
    private static readonly DateTimeOffset At = new(2026, 10, 28, 8, 0, 0, TimeSpan.Zero);

    /// <summary>One conversation of one mode, as a v2 run writes it; its one answer says "Hallo".</summary>
    private static string[] StoredV2Run()
    {
        EvalEvent[] events =
        [
            new ProtocolEvent(At, ProtocolEvent.CurrentVersion, "v2-cook", 1, "abc123", RunSplit.HeldOut, null, null,
                [new GraderInfo("wants-hallo", 1, GraderKind.FixedRule, null)],
                [new ModelInfo("cook", ModelLevel.Agent, SampleRole.Alpha, "claude-haiku-5-5")]),
            new ModeEvent(At, "cook", 1, [SampleToolset.Alpha], SampleRole.Alpha, null, 1),
            new QuestionEvent(At, "cook", 1, "pannekoek", 1, "sess_1", "Gee my resep"),
            new SseEvent(At, "cook", 1, "pannekoek", 1, "content_block_delta",
                Json("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hallo"}}""")),
            new TurnEvent(At, "cook", 1, "pannekoek", 1, "sess_1", "Gee my resep", "Hallo", null, null, 0, 0, [], 1.5,
                null, null),
            new ConversationEvent(At, "cook", 1, "pannekoek", null, 0, 1.5, [], TrialOutcome.Pass,
                new Metrics(null, null, null, null, null, null, null, null)),
            new ModeDoneEvent(At, "cook", 1, new RatesInfo(1, 1, 1, 0, 1)),
        ];
        return [.. events.Select(EvalProtocolJson.Line)];
    }

    /// <summary>Re-scores the stored lines with today's eval and hands back the lines it wrote.</summary>
    private static async Task<List<JsonElement>> RescoreAsync(string[] stored)
    {
        var sink = new StringWriter();
        var runner = new EvalRunner(new CannedSessionClient(), new ProtocolWriter(sink, new FixedClock(At)),
            new FixedClock(At), NullLogger<EvalRunner>.Instance);

        await runner.RescoreAsync(new HalloEval(), "run_new", "run_old", stored, CancellationToken.None);

        // The re-score writes version 1 until step 7c.2c, and copies the stored v2 lines as they were.
        return
        [
            .. sink.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => JsonDocument.Parse(line.TrimStart('@')).RootElement.Clone())
        ];
    }

    /// <summary>The lines of one event.</summary>
    private static IEnumerable<JsonElement> Of(IEnumerable<JsonElement> lines, string kind) =>
        lines.Where(line => line.GetProperty("event").GetString() == kind);

    /// <summary>A bit of JSON text as an element that outlives its document.</summary>
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    /// <summary>The v2 run's turn is graded again by today's grader.</summary>
    [Test]
    public async Task ItsConversation_IsGradedAgain()
    {
        var lines = await RescoreAsync(StoredV2Run());

        var grade = Of(lines, "conversation").Single().GetProperty("grades")[0];
        Assert.That((grade.GetProperty("grader").GetString(), grade.GetProperty("score").GetDouble()),
            Is.EqualTo(("wants-hallo", 1.0)));
    }

    /// <summary>Its first line gives the split and the models the new run names.</summary>
    [Test]
    public async Task ItsFirstLine_GivesTheSplitAndTheModels()
    {
        var lines = await RescoreAsync(StoredV2Run());

        var begin = Of(lines, "protocol").Single();
        Assert.That(begin.GetProperty("split").GetString(), Is.EqualTo("held_out"));
        Assert.That(begin.GetProperty("models")[0].GetProperty("model").GetString(), Is.EqualTo("claude-haiku-5-5"));
    }

    /// <summary>The retries its mode's end recorded carry over, as a v1 run's do.</summary>
    [Test]
    public async Task ItsModesRetries_CarryOver()
    {
        var lines = await RescoreAsync(StoredV2Run());

        Assert.That(Of(lines, "mode_done").Single().GetProperty("retried").GetInt32(), Is.EqualTo(1));
    }

    /// <summary>A grader that passes when every answer says "Hallo".</summary>
    private sealed class WantsHallo : IGrader
    {
        /// <inheritdoc />
        public string Name => "wants-hallo";

        /// <inheritdoc />
        public int Version => 1;

        /// <inheritdoc />
        public GraderKind Kind => GraderKind.FixedRule;

        /// <inheritdoc />
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

        /// <inheritdoc />
        public bool Required => true;

        /// <summary>Passes when every turn's answer holds "Hallo".</summary>
        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
        {
            bool held = trial.Transcript.Turns.All(turn =>
                EngineEvents.AnswerOf(turn)?.Contains("Hallo", StringComparison.Ordinal) == true);
            return ValueTask.FromResult(new Grade(Name, Version, held ? 1 : 0,
                held ? GradeOutcome.Pass : GradeOutcome.Fail, held ? "held" : "missing"));
        }
    }

    /// <summary>The eval as it is today: one mode, the one scenario, the one grader.</summary>
    private sealed class HalloEval : IEval
    {
        /// <inheritdoc />
        public string Name => "v2-cook";

        /// <inheritdoc />
        public int Version => 1;

        /// <inheritdoc />
        public string Proves => "A stored v2 run, to re-score.";

        /// <summary>One mode.</summary>
        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<EvalMode>>([
                new EvalMode("cook", SampleRole.Alpha, [SampleToolset.Alpha])
            ]);

        /// <inheritdoc />
        public IReadOnlyList<EvalScenario> Scenarios { get; } =
        [
            new EvalScenario("pannekoek", new EvalInput(null, "Africa/Johannesburg", ["Gee my resep"]), default, [],
                DatasetSplit.Dev),
        ];

        /// <inheritdoc />
        public IReadOnlyList<IGrader> Graders { get; } = [new WantsHallo()];

        /// <summary>No scores to keep.</summary>
        public EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades) => new NoScores();

        /// <summary>No facts about a turn.</summary>
        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => new(null, null, 0, 0, [], null);
    }

    /// <summary>An empty score document.</summary>
    private sealed record NoScores : EvalScores;
}
