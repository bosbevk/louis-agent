using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Running;
using LouisAgent.Evals.Tests.Protocol;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
using Microsoft.Extensions.Logging.Abstractions;

namespace LouisAgent.Evals.Tests.Running;

/// <summary>
/// A run asks for the dev scenarios by default, the held-out ones with <c>--split held-out</c>, or both with
/// <c>--split all</c>, and its first line says which.
/// </summary>
public class EvalRunnerSplitTests
{
    /// <summary>The time every line is written at.</summary>
    private static readonly DateTimeOffset At = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123);

    /// <summary>Runs the two-scenario eval once with the split given, and hands back every event it wrote.</summary>
    private static async Task<IReadOnlyList<EvalEvent>> Run(RunSplit split)
    {
        // One canned answer per session, so each scenario's one turn has something to read.
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

        return Written.Events(sink);
    }

    /// <summary>The scenarios that ran, by the conversation events.</summary>
    private static IEnumerable<string> Tasks(IEnumerable<EvalEvent> events) =>
        events.OfType<ConversationEvent>().Select(c => c.Task);

    /// <summary>The first line names the split.</summary>
    [TestCase(RunSplit.Dev)]
    [TestCase(RunSplit.HeldOut)]
    [TestCase(RunSplit.All)]
    public async Task TheRunsFirstLine_CarriesItsSplit(RunSplit split)
    {
        var events = await Run(split);

        Assert.That(events[0], Is.TypeOf<ProtocolEvent>());
        Assert.That(((ProtocolEvent)events[0]).Split, Is.EqualTo(split));
    }

    /// <summary>With no split, only the dev scenario runs, and the mode says it will run one conversation.</summary>
    [Test]
    public async Task ARunAsksForTheDevScenariosByDefault()
    {
        var events = await Run(RunSplit.Dev);

        Assert.That(Tasks(events), Is.EqualTo(new[] { "tune" }));
        Assert.That(events.OfType<ModeEvent>().Single().Conversations, Is.EqualTo(1));
    }

    /// <summary>A held-out run runs only the held-out scenario.</summary>
    [Test]
    public async Task HeldOutRunsOnlyTheHeldOutScenarios() =>
        Assert.That(Tasks(await Run(RunSplit.HeldOut)), Is.EqualTo(new[] { "release" }));

    /// <summary>A run of all runs both, and says it will run two conversations.</summary>
    [Test]
    public async Task AllRunsBoth()
    {
        var events = await Run(RunSplit.All);

        Assert.That(Tasks(events), Is.EqualTo(new[] { "tune", "release" }));
        Assert.That(events.OfType<ModeEvent>().Single().Conversations, Is.EqualTo(2));
    }

    /// <summary>Which scenarios each split asks for, as a table.</summary>
    [TestCase(RunSplit.Dev, DatasetSplit.Dev, true)]
    [TestCase(RunSplit.Dev, DatasetSplit.HeldOut, false)]
    [TestCase(RunSplit.HeldOut, DatasetSplit.Dev, false)]
    [TestCase(RunSplit.HeldOut, DatasetSplit.HeldOut, true)]
    [TestCase(RunSplit.All, DatasetSplit.Dev, true)]
    [TestCase(RunSplit.All, DatasetSplit.HeldOut, true)]
    public void ASelectionIncludesTheSplitsItNames(RunSplit selection, DatasetSplit split, bool included) =>
        Assert.That(selection.Includes(split), Is.EqualTo(included));

    /// <summary>An eval with one dev scenario and one held-out scenario, and nothing to grade.</summary>
    private sealed class TwoSplitsEval : IEval
    {
        /// <inheritdoc />
        public string Name => "stub-splits";

        /// <inheritdoc />
        public int Version => 1;

        /// <inheritdoc />
        public string Proves => "A dev and a held-out scenario.";

        /// <summary>One mode.</summary>
        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<EvalMode>>([
                new EvalMode("cook", SampleRole.Alpha, [SampleToolset.Alpha])
            ]);

        /// <inheritdoc />
        public IReadOnlyList<EvalScenario> Scenarios { get; } =
        [
            new EvalScenario("tune", new EvalInput(null, "Africa/Johannesburg", ["Een"]), default, [],
                DatasetSplit.Dev),
            new EvalScenario("release", new EvalInput(null, "Africa/Johannesburg", ["Twee"]), default, [],
                DatasetSplit.HeldOut),
        ];

        /// <inheritdoc />
        public IReadOnlyList<ICheck> Checks => [];

        /// <summary>No facts about a turn.</summary>
        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => new(null, null, 0, 0, [], null);
    }
}
