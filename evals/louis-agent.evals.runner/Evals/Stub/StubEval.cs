using LouisAgent.Choices;
using LouisAgent.Evals.Runner.Evals.Stub;
using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Runner.Messages;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Runner.Evals.Stub;

/// <summary>
/// The stub eval (F18 step 4.1): the C# port of benchmarks/stub/run_check.py. Two modes, two conversations, canned events and
/// facts, no session and no model. It proves the portal, not Puls.
/// </summary>
public sealed class StubEval : IEval
{
    /// <summary>The role the Python stub's modes name (its label only: the turns' roles come from the facts).</summary>
    public static readonly string Role = StubRole.Voice.Wire();

    /// <summary>The host the cook's canned answer reads (the Python stub's hosts).</summary>
    private const string CheckersHost = "www.checkers.co.za";

    public string Name => "stub";
    public string? Manual => "evals/louis-agent.evals.runner/Manuals/stub.md";
    public int Version => 1;
    public string Proves => MessageText.Format(RunnerMessage.StubProves);

    public ValueTask<IReadOnlyList<EvalMode>>
        ModesAsync(ISessionClient sessions, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<EvalMode>>(
        [
            new EvalMode(StubMode.Direct.Wire(), Role, [StubToolset.Web.Wire()]),
            new EvalMode(StubMode.Delegating.Wire(), Role, [StubToolset.Specialists.Wire(), StubToolset.Web.Wire()]),
        ]);

    public IReadOnlyList<EvalScenario> Scenarios { get; } =
    [
        new EvalScenario("bobotie", new EvalInput(null, "Africa/Johannesburg",
                [MessageText.Format(StubMessage.BobotieFirst), MessageText.Format(StubMessage.BobotieFollowUp)]),
            JsonDocument.Parse("{}").RootElement.Clone(), [], DatasetSplit.Dev),
        new EvalScenario("cottage-pie",
            new EvalInput(null, "Africa/Johannesburg", [MessageText.Format(StubMessage.CottagePie)]),
            JsonDocument.Parse("{}").RootElement.Clone(), [], DatasetSplit.Dev),
    ];

    /// <summary>The stub's graders: no turn errored, and each turn was answered within a minute (a stuck run, the only slow one).</summary>
    public IReadOnlyList<ICheck> Checks { get; } =
        [new NoErrorCheck(), new WithinTimeCheck(TimeSpan.FromMinutes(1))];

    /// <summary>The canned facts the Python stub writes: the cook answers with one call and three specialist events, and a follow-up continues it.</summary>
    public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode)
    {
        bool cook = mode.Toolsets.Contains(StubToolset.Specialists.Wire(), StringComparer.Ordinal);
        return new TurnFacts(
            Role: cook ? StubRole.Specialist.Wire() : null,
            Continued: cook && turn.Turn > 1 ? "sub-1" : null,
            AgentCalls: cook ? 1 : 0,
            SpecialistCalls: cook ? 3 : 0,
            Hosts: cook ? [CheckersHost] : [],
            Error: null);
    }
}
