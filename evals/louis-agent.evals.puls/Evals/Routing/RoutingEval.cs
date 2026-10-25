using LouisAgent.Core.Messages;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Puls.Messages;
using LouisAgent.Evals.Trials;
using LouisAgent.Evals.Puls.Evals.Routing.Scoring;
using LouisAgent.Evals.Puls.Shared;
using LouisAgent.Evals.Puls.Shared.Expectations;
using LouisAgent.Evals.Puls.Shared.Grading;
using LouisAgent.Evals.Puls.Shared.Turns;

namespace LouisAgent.Evals.Puls.Evals.Routing;

/// <summary>
/// The routing check as an eval (F18 step 5.3): eighteen questions (twelve for the dev split, two of each kind, and six held out, one of each), each in a session of its own, in two modes: the
/// baseline, one agent with every toolset the instance has but the delegating ones, and the delegated, Puls as built. A meal must go to
/// the cook; every other question is right when Puls answers it itself. The scenarios are the <c>puls-routing</c> dataset.
/// </summary>
public sealed class RoutingEval : IEval
{
    // One required route, the harness's RoutedToGrader reading `routes_to`. A no-error grader would add nothing: a one-turn question is routed right only without an error (RoutingScorer).
    private static readonly IReadOnlyList<IGrader> GraderList = [PulsRoute.Grader()];

    public string Name => "puls-routing";
    public string? Identity => "puls";
    public int Version => 2;
    public string Proves => MessageText.Format(PulsEvalMessage.RoutingProves);

    public async ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions, CancellationToken cancellationToken)
    {
        var baseline = RoutingScorer.Baseline(await sessions.ToolsetsAsync(cancellationToken));
        return [new EvalMode("baseline", PulsVoice.Role, baseline), new EvalMode("delegated", PulsVoice.Role, RoutingSetup.Delegated)];
    }

    private static readonly Dataset Loaded = Load();

    public IReadOnlyList<EvalScenario> Scenarios { get; } = Loaded.Scenarios;

    public DatasetManifest? Dataset => Loaded.Manifest;

    /// <summary>The dataset, with every scenario's route read once: a scenario without a readable `routes_to` refuses the runner before a run.</summary>
    private static Dataset Load()
    {
        var dataset = DatasetLoader.Load(EvalFiles.DatasetOf(typeof(RoutingEval)), GraderList);
        foreach (var scenario in dataset.Scenarios) ExpectedRoute.Of(scenario);
        return dataset;
    }

    public IReadOnlyList<IGrader> Graders => GraderList;

    public EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades) =>
        RoutingScorer.Score(ExpectedRoute.Of(trial.Scenario), TurnView.From(trial.Transcript.Turns[0]), PulsModes.CanDelegate(trial.Mode));

    public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => PulsTurnFacts.Of(turn);
}
