using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Models;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Running;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.Logging.Abstractions;

namespace LouisAgent.Evals.Tests.Grading.Models;

// F18 step 6.1: a model grader in a whole run and in its re-score. The run goes on with no judge (Unknown, "no judge configured"), and what judged (the model, the
// prompt's hash) is on the grade line of the run and of a re-score of it. Written first: neither the grader nor the fields existed.
public class ModelGraderInARunTests
{
    private static readonly DateTimeOffset At = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123);

    private static readonly IReadOnlyList<SessionEvent> Answer =
    [
        SessionEvent.From("content_block_delta",
            JsonDocument
                .Parse(
                    """{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Bobotie vir vier"}}""")
                .RootElement.Clone())
    ];

    private static ModelEval Eval(bool withJudge) => new(new EvaluatorGrader(
        new FakeEvaluator(new BooleanMetric("meal", true, "feeds four")),
        withJudge ? new ConfiguredJudge(new ChatConfiguration(new FakeChatClient()), "qwen3:8b") : null,
        "meal-for-people", 1, "abc123def456"));

    private static async Task<string[]> RunAsync(ModelEval eval)
    {
        var sink = new StringWriter();
        var runner = new EvalRunner(new CannedSessionClient(Answer), new ProtocolWriter(sink, new FixedClock(At)),
            new FixedClock(At), NullLogger<EvalRunner>.Instance);
        await runner.RunAsync(eval, new EvalRun("run_old", 1), CancellationToken.None);
        return Lines(sink);
    }

    [Test]
    public async Task ARunWithNoJudge_GoesOn_AndTheModelGradersGradeIsUnknown()
    {
        string[] lines = await RunAsync(Eval(withJudge: false));

        var grade = Conversation(lines).GetProperty("grades")[0];
        Assert.That(grade.GetProperty("outcome").GetString(), Is.EqualTo("Unknown"));
        Assert.That(grade.GetProperty("reason").GetString(), Is.EqualTo("no judge configured"));
        Assert.That(Conversation(lines).GetProperty("outcome").GetString(), Is.EqualTo("Unknown"),
            "the run finished and said it could not tell");
    }

    [Test]
    public async Task TheGradeLineSaysWhatJudged_AndAReScoreOfTheRunSaysItAgain()
    {
        string[] stored = await RunAsync(Eval(withJudge: true));
        var sink = new StringWriter();
        var runner = new EvalRunner(new NoSessionClient(), new ProtocolWriter(sink, new FixedClock(At)),
            new FixedClock(At), NullLogger<EvalRunner>.Instance);

        await runner.RescoreAsync(Eval(withJudge: true), "run_new", "run_old", stored, CancellationToken.None);

        foreach (var lines in new[] { stored, Lines(sink) })
        {
            var grade = Conversation(lines).GetProperty("grades")[0];
            Assert.That(grade.GetProperty("outcome").GetString(), Is.EqualTo("Pass"));
            Assert.That(grade.GetProperty("judge_model").GetString(), Is.EqualTo("qwen3:8b"));
            Assert.That(grade.GetProperty("judge_prompt_hash").GetString(), Is.EqualTo("abc123def456"));
        }
    }

    private static string[] Lines(StringWriter sink) =>
        sink.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);

    private static JsonElement Conversation(IEnumerable<string> lines) =>
        lines.Select(line => JsonDocument.Parse(line.TrimStart('@')).RootElement.Clone())
            .Single(e => e.GetProperty("event").GetString() == "conversation");

    /// <summary>An eval of one scenario and one model grader, the shape of the cook's judge before it has a prompt.</summary>
    private sealed class ModelEval(IGrader grader) : IEval
    {
        public string Name => "model-eval";
        public int Version => 1;
        public string Proves => "A model grader in a run.";

        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<EvalMode>>([
                new EvalMode("cook", SampleRole.Alpha, [SampleToolset.Alpha])
            ]);

        public IReadOnlyList<EvalScenario> Scenarios { get; } =
        [
            new EvalScenario("bobotie", new EvalInput(null, "Africa/Johannesburg", ["Gee my 'n resep vir vier"]),
                default, [], DatasetSplit.Dev)
        ];

        public IReadOnlyList<IGrader> Graders { get; } = [grader];
        public EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades) => new Scores();
        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => new(null, null, 0, 0, [], null);
    }

    private sealed record Scores : EvalScores;
}
