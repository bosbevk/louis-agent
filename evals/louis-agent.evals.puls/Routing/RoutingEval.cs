using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Puls.Grading.FixedRules;
using LouisAgent.Evals.Puls.Turns;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Puls.Routing;

/// <summary>
/// The routing check as an eval (F18 step 5.3): twelve questions, two of each kind, each in a session of its own, in two modes: the
/// baseline, one agent with every toolset the instance has but the delegating ones, and the delegated, Puls as built. A meal must go to
/// the cook; every other question is right when Puls answers it itself. The scenarios are the <c>puls-routing</c> dataset.
/// </summary>
public sealed class RoutingEval : IEval
{
    // One required route. A no-error grader would add nothing: a one-turn question is routed right only without an error (RoutingScorer).
    private static readonly IReadOnlyList<IGrader> GraderList = [new RoutingRoutedRight()];

    public string Name => "puls-routing";
    public string? Identity => "puls";
    public int Version => 1;
    public string Proves => "Puls sends each kind of question to the right place: a meal to its cook, the rest answered itself.";

    public async ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions, CancellationToken cancellationToken)
    {
        var baseline = RoutingScorer.Baseline(await sessions.ToolsetsAsync(cancellationToken));
        return [new EvalMode("baseline", PulsVoice.Role, baseline), new EvalMode("delegated", PulsVoice.Role, RoutingSetup.Delegated)];
    }

    public IReadOnlyList<EvalScenario> Scenarios { get; } =
        DatasetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Datasets", "puls-routing"), GraderList).Scenarios;

    public IReadOnlyList<IGrader> Graders => GraderList;

    public EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades) =>
        RoutingScorer.Score(trial.Scenario.Id, TurnView.From(trial.Transcript.Turns[0]), RoutingScorer.Delegating(trial.Mode));

    public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => PulsTurnFacts.Of(turn);
}
