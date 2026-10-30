using LouisAgent.Choices;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Datasets.Manifest;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Puls.Messages;
using LouisAgent.Evals.Trials;
using LouisAgent.Evals.Puls.Evals.Routing.Scoring;
using LouisAgent.Evals.Puls.Shared;
using LouisAgent.Evals.Puls.Shared.Expectations;
using LouisAgent.Evals.Puls.Shared.Turns;

namespace LouisAgent.Evals.Puls.Evals.Routing;

/// <summary>
/// The routing check as an eval: eighteen questions (twelve for the dev split, two of each kind, and six held out,
/// one of each), each in a session of its own, in two modes: the baseline, one agent with every toolset the instance
/// has but the delegating ones, and the delegated, Puls as built. A meal must go to the cook; every other question is
/// right when Puls answers it itself. The scenarios are the <c>puls-routing</c> dataset.
/// </summary>
public sealed class RoutingEval : IEval
{
    /// <summary>
    /// One required route, the general <c>routed-to</c> reading <c>routes_to</c>, where only the specialists hand a
    /// question on. A no-error check would add nothing: a one-turn question is routed right only without an error.
    /// </summary>
    private static readonly IReadOnlyList<ICheck> GraderList =
        [RoutedToCheck.ByScenarioKey(ExpectedRoute.Key, [PulsToolset.Specialists.Wire()])];

    /// <inheritdoc />
    public string Name => PulsEvalName.Routing.Wire();

    /// <inheritdoc />
    public string Manual => "evals/louis-agent.evals.puls/Manuals/routing.md";

    /// <inheritdoc />
    public int Version => 2;

    /// <inheritdoc />
    public string Proves => MessageText.Format(PulsEvalMessage.RoutingProves);

    /// <summary>
    /// The baseline, every toolset the instance has but the delegating ones, and the delegated, Puls as built.
    /// </summary>
    public async ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions,
        CancellationToken cancellationToken)
    {
        var baseline = RoutingScorer.Baseline(await sessions.ToolsetsAsync(cancellationToken));
        return
        [
            new EvalMode("baseline", PulsVoice.Role, baseline),
            new EvalMode("delegated", PulsVoice.Role, RoutingSetup.Delegated)
        ];
    }

    /// <summary>The dataset, loaded once with the check above.</summary>
    private static readonly Dataset Loaded = Load();

    /// <inheritdoc />
    public IReadOnlyList<EvalScenario> Scenarios { get; } = Loaded.Scenarios;

    /// <inheritdoc />
    public DatasetManifest Dataset => Loaded.Manifest;

    /// <summary>
    /// The dataset, with every scenario's route read once: a scenario without a readable <c>routes_to</c> stops the
    /// runner before a run.
    /// </summary>
    private static Dataset Load()
    {
        var dataset = DatasetLoader.Load(EvalFiles.DatasetOf(typeof(RoutingEval)), GraderList);
        foreach (var scenario in dataset.Scenarios) ExpectedRoute.Of(scenario);
        return dataset;
    }

    /// <inheritdoc />
    public IReadOnlyList<ICheck> Checks => GraderList;

    /// <summary>What Puls's own turn view reads from a turn.</summary>
    public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => PulsTurnFacts.Of(turn);
}
