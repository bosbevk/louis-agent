using LouisAgent.Core.Messages;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Puls.Expectations;
using LouisAgent.Evals.Puls.Grading.FixedRules;
using LouisAgent.Evals.Puls.Messages;
using LouisAgent.Evals.Puls.Turns;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Puls.Cook;

/// <summary>
/// The cook check as an eval (F18 step 5.3): a meal in Afrikaans or English, asked of Puls with the cook's own tools and of Puls with its
/// cook, in a town with a shop and one without, with a budget and a follow-up, and a web question the cook is not for (the control). The
/// scenarios are the <c>puls-cook</c> dataset, each stating its requirements in `expected` (F18 step 5.4); the scores are the Python check's, key for key.
/// </summary>
public sealed class CookEval : IEval
{
    // Static, so the scenarios can load with them: the dataset checks its expected keys against these graders when it loads.
    private static readonly IReadOnlyList<IGrader> GraderList =
        [new CookNoError(), PulsRoute.Grader(), new WithinBudget(), new PricedAtShop(), new ServesAsAsked()];

    public string Name => "puls-cook";
    public string? Identity => "puls";
    public int Version => 2;
    public string Proves => MessageText.Format(PulsEvalMessage.CookProves);

    public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<EvalMode>>(CookSetup.Modes.Select(mode => new EvalMode(mode.Key, PulsVoice.Role, mode.Value)).ToList());

    public IReadOnlyList<EvalScenario> Scenarios { get; } = Load();

    /// <summary>The dataset, with every scenario's expectations read once: a scenario that asks for something unreadable refuses the runner before a run.</summary>
    private static IReadOnlyList<EvalScenario> Load()
    {
        var scenarios = DatasetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Datasets", "puls-cook"), GraderList).Scenarios;
        foreach (var scenario in scenarios) CookExpectations.Of(scenario);
        return scenarios;
    }

    public IReadOnlyList<IGrader> Graders => GraderList;

    public EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades) => CookScorer.ScoreTrial(trial);

    public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => PulsTurnFacts.Of(turn);
}
