using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Running;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
using Microsoft.Extensions.Logging.Abstractions;

namespace LouisAgent.Evals.Tests.Running;

// F18 step 5.4: a run asks for the dev scenarios by default, the held-out ones with --split held-out, or both with --split all.
public class EvalRunnerSplitTests
{
    private static readonly DateTimeOffset At = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123);

    private static async Task<List<JsonElement>> Run(SplitSelection split)
    {
        var turn = new[]
        {
            SessionEvent.From("content_block_delta",
                JsonDocument
                    .Parse("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hallo"}}""")
                    .RootElement.Clone())
        };
        var sink = new StringWriter();
        var runner = new EvalRunner(new CannedSessionClient(turn, turn), new ProtocolWriter(sink, new FixedClock(At)),
            new FixedClock(At),
            NullLogger<EvalRunner>.Instance);

        await runner.RunAsync(new TwoSplitsEval(), new EvalRun("run_1", 1, Split: split), CancellationToken.None);

        return sink.ToString().Split((char)10, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => JsonDocument.Parse(l.TrimStart('@')).RootElement.Clone()).ToList();
    }

    private static IEnumerable<string> Tasks(IEnumerable<JsonElement> lines) =>
        lines.Where(l => l.GetProperty("event").GetString() == "conversation")
            .Select(l => l.GetProperty("task").GetString()!);

    [TestCase(SplitSelection.Dev, "dev")]
    [TestCase(SplitSelection.HeldOut, "held_out")]
    [TestCase(SplitSelection.All, "all")]
    public async Task TheRunsFirstLine_CarriesItsSplit(SplitSelection split, string wire)
    {
        var lines = await Run(split);

        Assert.That(lines[0].GetProperty("event").GetString(), Is.EqualTo("protocol"));
        Assert.That(lines[0].GetProperty("split").GetString(), Is.EqualTo(wire));
    }

    [Test]
    public async Task ARunAsksForTheDevScenariosByDefault()
    {
        var lines = await Run(SplitSelection.Dev);

        Assert.That(Tasks(lines), Is.EqualTo(new[] { "tune" }));
        Assert.That(
            lines.Single(l => l.GetProperty("event").GetString() == "mode").GetProperty("conversations").GetInt32(),
            Is.EqualTo(1));
    }

    [Test]
    public async Task HeldOutRunsOnlyTheHeldOutScenarios() =>
        Assert.That(Tasks(await Run(SplitSelection.HeldOut)), Is.EqualTo(new[] { "release" }));

    [Test]
    public async Task AllRunsBoth()
    {
        var lines = await Run(SplitSelection.All);

        Assert.That(Tasks(lines), Is.EqualTo(new[] { "tune", "release" }));
        Assert.That(
            lines.Single(l => l.GetProperty("event").GetString() == "mode").GetProperty("conversations").GetInt32(),
            Is.EqualTo(2));
    }

    [TestCase(SplitSelection.Dev, DatasetSplit.Dev, true)]
    [TestCase(SplitSelection.Dev, DatasetSplit.HeldOut, false)]
    [TestCase(SplitSelection.HeldOut, DatasetSplit.Dev, false)]
    [TestCase(SplitSelection.HeldOut, DatasetSplit.HeldOut, true)]
    [TestCase(SplitSelection.All, DatasetSplit.Dev, true)]
    [TestCase(SplitSelection.All, DatasetSplit.HeldOut, true)]
    public void ASelectionIncludesTheSplitsItNames(SplitSelection selection, DatasetSplit split, bool included) =>
        Assert.That(selection.Includes(split), Is.EqualTo(included));

    private sealed class TwoSplitsEval : IEval
    {
        public string Name => "stub-splits";
        public int Version => 1;
        public string Proves => "A dev and a held-out scenario.";

        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<EvalMode>>([
                new EvalMode("cook", SampleRole.Alpha, [SampleToolset.Alpha])
            ]);

        public IReadOnlyList<EvalScenario> Scenarios { get; } =
        [
            new EvalScenario("tune", new EvalInput(null, "Africa/Johannesburg", ["Een"]), default, [],
                DatasetSplit.Dev),
            new EvalScenario("release", new EvalInput(null, "Africa/Johannesburg", ["Twee"]), default, [],
                DatasetSplit.HeldOut),
        ];

        public IReadOnlyList<IGrader> Graders => [];
        public EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades) => new NoScores();
        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => new(null, null, 0, 0, [], null);
    }

    private sealed record NoScores : EvalScores;
}
