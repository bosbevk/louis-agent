using LouisAgent.Choices;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Datasets.Manifest;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Labels;
using LouisAgent.Evals.Puls.Messages;
using LouisAgent.Evals.Trials;
using LouisAgent.Evals.Puls.Evals.Cook.Expectations;
using LouisAgent.Evals.Grading.Human;
using LouisAgent.Evals.Grading.Models;
using LouisAgent.Evals.Puls.Evals.Cook.Grading;
using LouisAgent.Evals.Puls.Evals.Cook.Grading.Judges;
using LouisAgent.Evals.Puls.Evals.Cook.Grading.WhereToBuy;
using LouisAgent.Evals.Puls.Shared;
using LouisAgent.Evals.Puls.Shared.Expectations;
using LouisAgent.Evals.Puls.Shared.Turns;

namespace LouisAgent.Evals.Puls.Evals.Cook;

/// <summary>
/// Asks Puls for a meal, in Afrikaans or English, once with the cook's tools in its own hands and once with its cook
/// to hand off to. The scenarios cover a shop's search pages, a town where the shop has no branch, no shop chosen, a
/// budget, a follow-up, and a web question the cook isn't for. Each scenario says what it expects in the
/// <c>puls-cook</c> dataset.
/// </summary>
public sealed class CookEval : IEval
{
    /// <summary>
    /// The graders, static so the scenarios can load with them: the dataset checks its expected keys against them.
    /// </summary>
    private static readonly IReadOnlyList<ICheck> GraderList = BuildGraders();

    /// <summary>The dataset, loaded once with the graders above.</summary>
    private static readonly Dataset Loaded = Load();

    /// <summary>
    /// The fixed rules first, then the model judge and a person's review of the same question, made for the judge it
    /// reviews (F18 step 6a).
    /// </summary>
    private static IReadOnlyList<ICheck> BuildGraders()
    {
        var judge = MealForPeopleCheck.Create(() => JudgeClients.FromEnvironment());
        return
        [
            new CookNoError(), RoutedToCheck.ByScenarioKey(ExpectedRoute.Key, [PulsToolset.Specialists.Wire()]),
            new WithinBudget(), new EstimatesAtShop(), new UntooledShops(), new RecipeImage(), new ServesAsAsked(),
            judge,
            HumanReviewCheck.For(judge, MealForPeoplePrompt.Question)
        ];
    }

    /// <inheritdoc />
    public string Name => PulsEvalName.Cook.Wire();

    /// <inheritdoc />
    public string Manual => "evals/louis-agent.evals.puls/Manuals/cook.md";

    /// <inheritdoc />
    public int Version => 10;

    /// <inheritdoc />
    public string Proves => MessageText.Format(PulsEvalMessage.CookProves);

    /// <summary>Puls's voice, with the cook's tools in one mode and the cook to hand off to in the other.</summary>
    public ValueTask<IReadOnlyList<EvalMode>>
        ModesAsync(ISessionClient sessions, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<EvalMode>>(CookSetup.Modes
            .Select(mode => new EvalMode(mode.Key, PulsVoice.Role, mode.Value)).ToList());

    /// <inheritdoc />
    public IReadOnlyList<EvalScenario> Scenarios { get; } = Loaded.Scenarios;

    /// <inheritdoc />
    public DatasetManifest Dataset => Loaded.Manifest;

    /// <summary>A person's labels of stored answers, which <c>meal-for-people</c> is calibrated against.</summary>
    public IReadOnlyList<LabeledAnswer> Labels => Loaded.Labels;

    /// <inheritdoc />
    public IReadOnlyList<ICheck> Checks => GraderList;

    /// <summary>What Puls's own turn view reads from a turn.</summary>
    public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => PulsTurnFacts.Of(turn);

    /// <summary>
    /// Loads the dataset and reads every scenario's expectations once, so a scenario that asks for something
    /// unreadable stops the runner before a run starts.
    /// </summary>
    private static Dataset Load()
    {
        var dataset = DatasetLoader.Load(EvalFiles.DatasetOf(typeof(CookEval)), GraderList);
        foreach (var scenario in dataset.Scenarios) CookExpectations.Of(scenario);
        return dataset;
    }
}
