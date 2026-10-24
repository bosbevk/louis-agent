using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Runner.Messages;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Runner.Stub;

/// <summary>
/// The stub eval (F18 step 4.1): the C# port of benchmarks/stub/run_check.py. Two modes, two conversations, canned events and
/// facts, no session and no model. It proves the portal, not Puls.
/// </summary>
public sealed class StubEval : IEval
{
    /// <summary>The role the Python stub's modes name (its label only: the turns' roles come from the facts).</summary>
    public const string Role = "puls/voice-af";

    /// <summary>The host the cook's canned answer reads (the Python stub's hosts).</summary>
    private const string CheckersHost = "www.checkers.co.za";

    public string Name => "stub";
    public string? Identity => null;
    public int Version => 1;
    public string Proves => MessageText.Format(RunnerMessage.StubProves);

    public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<EvalMode>>(
        [
            new EvalMode("puls-only", Role, ["web"]),
            new EvalMode("puls-cook", Role, ["specialists", "web"]),
        ]);

    public IReadOnlyList<EvalScenario> Scenarios { get; } =
    [
        new EvalScenario("bobotie", new EvalInput(null, "Africa/Johannesburg",
                [MessageText.Format(StubMessage.BobotieFirst), MessageText.Format(StubMessage.BobotieFollowUp)]),
            JsonDocument.Parse("{}").RootElement.Clone(), [], DatasetSplit.Dev),
        new EvalScenario("cottage-pie", new EvalInput(null, "Africa/Johannesburg", [MessageText.Format(StubMessage.CottagePie)]),
            JsonDocument.Parse("{}").RootElement.Clone(), [], DatasetSplit.Dev),
    ];

    public IReadOnlyList<IGrader> Graders { get; } = [new StubGrader()];

    /// <summary>The stub's run is fixed, so its route is always right, and the score counts the turns (the Python's scores).</summary>
    public EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades) => new StubScores(RoutedRight: true, Turns: trial.Transcript.Turns.Count);

    /// <summary>The canned facts the Python stub writes: the cook answers with one call and three specialist events, and a follow-up continues it.</summary>
    public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode)
    {
        bool cook = mode.Toolsets.Contains("specialists", StringComparer.Ordinal);
        return new TurnFacts(
            Role: cook ? "puls-cook" : null,
            Continued: cook && turn.Turn > 1 ? "sub-1" : null,
            PulsCalls: cook ? 1 : 0,
            SpecialistCalls: cook ? 3 : 0,
            Hosts: cook ? [CheckersHost] : [],
            Error: null);
    }
}
