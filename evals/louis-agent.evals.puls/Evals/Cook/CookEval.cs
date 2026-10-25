using LouisAgent.Core.Messages;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Puls.Messages;
using LouisAgent.Evals.Trials;
using LouisAgent.Evals.Puls.Evals.Cook.Expectations;
using LouisAgent.Evals.Puls.Evals.Cook.Grading;
using LouisAgent.Evals.Puls.Evals.Cook.Scoring;
using LouisAgent.Evals.Puls.Shared;
using LouisAgent.Evals.Puls.Shared.Grading;
using LouisAgent.Evals.Puls.Shared.Turns;

namespace LouisAgent.Evals.Puls.Evals.Cook;

/// <summary>
/// The cook check as an eval (F18 step 5.3): a meal in Afrikaans or English, asked of Puls with the cook's own tools and of Puls with its
/// cook, priced at PnP and Woolworths from their search pages, in a town where the shop has no branch, with no shop chosen, with a budget and a follow-up,
/// and a web question the cook is not for (the control). The scenarios are the <c>puls-cook</c> dataset, each stating its requirements in `expected`
/// (F18 step 5.4); the scores keep the keys of the Python check this replaced.
/// </summary>
public sealed class CookEval : IEval
{
    // Static, so the scenarios can load with them: the dataset checks its expected keys against these graders when it loads.
    private static readonly IReadOnlyList<IGrader> GraderList =
        [new CookNoError(), PulsRoute.Grader(), new WithinBudget(), new EstimatesAtShop(), new RecipeImage(), new ServesAsAsked()];

    public string Name => "puls-cook";
    public string? Identity => "puls";
    public int Version => 7;
    public string Proves => MessageText.Format(PulsEvalMessage.CookProves);

    public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<EvalMode>>(CookSetup.Modes.Select(mode => new EvalMode(mode.Key, PulsVoice.Role, mode.Value)).ToList());

    private static readonly Dataset Loaded = Load();

    public IReadOnlyList<EvalScenario> Scenarios { get; } = Loaded.Scenarios;

    public DatasetManifest? Dataset => Loaded.Manifest;

    /// <summary>The dataset, with every scenario's expectations read once: a scenario that asks for something unreadable refuses the runner before a run.</summary>
    private static Dataset Load()
    {
        var dataset = DatasetLoader.Load(EvalFiles.DatasetOf(typeof(CookEval)), GraderList);
        foreach (var scenario in dataset.Scenarios) CookExpectations.Of(scenario);
        return dataset;
    }

    public IReadOnlyList<IGrader> Graders => GraderList;

    public EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades) => CookScorer.ScoreTrial(trial);

    public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => PulsTurnFacts.Of(turn);
}
