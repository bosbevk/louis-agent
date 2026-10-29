using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Models;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Running;
using LouisAgent.Evals.Tests.Protocol;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.Logging.Abstractions;

namespace LouisAgent.Evals.Tests.Grading.Models;

/// <summary>
/// A model grader in a whole run and in its re-score. The run goes on with no judge (Unknown, "no judge
/// configured"), and what judged (the model, the prompt's hash) is on the grade of the run and of a re-score of it.
/// </summary>
public class ModelGraderInARunTests
{
    /// <summary>The time every line carries.</summary>
    private static readonly DateTimeOffset At = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123);

    /// <summary>The one answer: a meal for four.</summary>
    private static readonly IReadOnlyList<SessionEvent> Answer =
    [
        SessionEvent.From("content_block_delta",
            JsonDocument
                .Parse("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Bobotie """
                       + """vir vier"}}""")
                .RootElement.Clone())
    ];

    /// <summary>The eval with its model grader, with a judge or without one.</summary>
    private static ModelEval Eval(bool withJudge) => new(new EvaluatorGrader(
        new FakeEvaluator(new BooleanMetric("meal", true, "feeds four")),
        withJudge ? new ConfiguredJudge(new ChatConfiguration(new FakeChatClient()), "qwen3:8b") : null,
        "meal-for-people", 1, "abc123def456"));

    /// <summary>Runs the eval over the canned answer and hands back the lines it wrote.</summary>
    private static async Task<string[]> RunAsync(ModelEval eval)
    {
        var sink = new StringWriter();
        var runner = new EvalRunner(new CannedSessionClient(Answer), new ProtocolWriter(sink, new FixedClock(At)),
            new FixedClock(At), NullLogger<EvalRunner>.Instance);

        await runner.RunAsync(eval, new EvalRun("run_old", 1), CancellationToken.None);

        return Written.Lines(sink);
    }

    /// <summary>The one conversation among the lines.</summary>
    private static ConversationEvent Conversation(IEnumerable<string> lines) =>
        Written.Events(lines).OfType<ConversationEvent>().Single();

    /// <summary>With no judge the run goes on, and the model grader's grade is Unknown.</summary>
    [Test]
    public async Task ARunWithNoJudge_GoesOn_AndTheModelGradersGradeIsUnknown()
    {
        var conversation = Conversation(await RunAsync(Eval(withJudge: false)));

        Assert.That((conversation.Grades[0].Outcome, conversation.Grades[0].Reason),
            Is.EqualTo((GradeOutcome.Unknown, "no judge configured")));
        Assert.That(conversation.Outcome, Is.EqualTo(TrialOutcome.Unknown),
            "the run finished and said it could not tell");
    }

    /// <summary>The grade says what judged it, and a re-score of the run says it again.</summary>
    [Test]
    public async Task TheGradeLineSaysWhatJudged_AndAReScoreOfTheRunSaysItAgain()
    {
        string[] stored = await RunAsync(Eval(withJudge: true));
        var sink = new StringWriter();
        var runner = new EvalRunner(new NoSessionClient(), new ProtocolWriter(sink, new FixedClock(At)),
            new FixedClock(At), NullLogger<EvalRunner>.Instance);

        await runner.RescoreAsync(Eval(withJudge: true), "run_new", "run_old", stored, CancellationToken.None);

        foreach (var lines in new[] { stored, Written.Lines(sink) })
        {
            var grade = Conversation(lines).Grades[0];
            Assert.That((grade.Outcome, grade.JudgeModel, grade.JudgePromptHash),
                Is.EqualTo((GradeOutcome.Pass, "qwen3:8b", "abc123def456")));
        }
    }

    /// <summary>
    /// An eval of one scenario and one model grader, the shape of the cook's judge before it has a prompt.
    /// </summary>
    private sealed class ModelEval(IGrader grader) : IEval
    {
        /// <inheritdoc />
        public string Name => "model-eval";

        /// <inheritdoc />
        public int Version => 1;

        /// <inheritdoc />
        public string Proves => "A model grader in a run.";

        /// <summary>One mode.</summary>
        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<EvalMode>>([
                new EvalMode("cook", SampleRole.Alpha, [SampleToolset.Alpha])
            ]);

        /// <inheritdoc />
        public IReadOnlyList<EvalScenario> Scenarios { get; } =
        [
            new EvalScenario("bobotie", new EvalInput(null, "Africa/Johannesburg", ["Gee my 'n resep vir vier"]),
                default, [], DatasetSplit.Dev)
        ];

        /// <inheritdoc />
        public IReadOnlyList<IGrader> Graders { get; } = [grader];

        /// <summary>No facts about a turn.</summary>
        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => new(null, null, 0, 0, [], null);
    }
}
