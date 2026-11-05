using LouisAgent.Choices;
using LouisAgent.Core.Skills.Headers;
using LouisAgent.Evals;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Conformance;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Common.Compare.Readers;
using LouisAgent.Evals.Grading.Human;
using LouisAgent.Evals.Grading.Models.Rubrics;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Readers;
using LouisAgent.Evals.Running;
using LouisAgent.Identities.Puls.Evals;
using LouisAgent.Identities.Puls.Evals.Checks.Cook;
using LouisAgent.Identities.Puls.Evals.Checks.Cook.Scoring;
using LouisAgent.Identities.Puls.Evals.Checks.Shared;
using LouisAgent.Packs.Recipes.Checks;
using LouisAgent.Packs.WhereToBuy.Checks;
using LouisAgent.SessionStream;
using Microsoft.Extensions.Logging.Abstractions;
using static LouisAgent.Identities.Puls.Tests.Evals.Checks.PulsTrials;

namespace LouisAgent.Identities.Puls.Tests.Evals.Recipes;

/// <summary>
/// Puls's recipes eval as a folder of files in its evals project: thirteen questions for a recipe, each in a session of
/// its own, asked of Puls as built. The cook should read a page with ReadRecipe, link a site we may read, show the
/// page's photo, and write the page's amounts and steps, which a judge told what ReadRecipe gave checks.
/// </summary>
public class RecipesEvalTests
{
    /// <summary>Where the folder sits in the repository.</summary>
    private const string Source = "demos/puls/evals/louis-agent.identity.puls.evals/Evals/Recipes";

    /// <summary>
    /// The rubric's hash, the one the judge's grades carry on every platform (<c>RubricHash</c>: line endings made LF),
    /// of the body after the header, so a change to the rubric's words shows here.
    /// </summary>
    private const string PinnedRubricHash = "6e1915c60132";

    /// <summary>The dishes to work on, as their scenarios' ids.</summary>
    private static readonly string[] Dev =
    [
        "recipe-bobotie", "recipe-melktert", "recipe-chakalaka", "recipe-potjiekos", "recipe-malva-pudding",
        "recipe-vetkoek", "recipe-boerewors-rolls", "recipe-pap-en-sous", "recipe-hoenderpastei", "recipe-koeksisters"
    ];

    /// <summary>The dishes held out for a release check.</summary>
    private static readonly string[] HeldOut = ["recipe-umngqusho", "recipe-sosaties", "recipe-bredie"];

    /// <summary>It is the identity's eval, under its dataset's name, at its version, with a manual beside it.</summary>
    [Test]
    public void ItIsPulssRecipesEval_UnderItsDatasetsName()
    {
        var eval = Load();

        Assert.That((eval.Name, eval.Dataset.Name, eval.Identity, eval.Version, eval.Dataset.Version),
            Is.EqualTo(("puls-recipes", "puls-recipes", PulsIdentityName.Puls.Wire(), 6, 1)));
        Assert.That(eval.Proves, Does.StartWith("Puls's cook reads a recipe from a real page"));
        Assert.That(eval.Manual, Is.EqualTo($"{Source}/manual.md"));
    }

    /// <summary>It has the one mode: Puls as built, which hands a meal to its cook.</summary>
    [Test]
    public async Task ItsOneMode_IsPulsAsBuilt()
    {
        var modes = await Load().ModesAsync(null!, CancellationToken.None);

        Assert.That(modes.Select(m => (m.Name, m.Role, string.Join(",", m.Toolsets))),
            Is.EqualTo(new[] { (CookMode.Cook.Wire(), Role, "specialists,web") }));
    }

    /// <summary>
    /// The fixed rules that must hold come first, the list of unpriced items, the judge and its review, which are
    /// measured but never fail a run, last; the image and unpriced rules keep the names they have in the cook's eval.
    /// </summary>
    [Test]
    public void ItsChecks_AreTheRequiredRulesFirst_ThenTheJudgeAndItsReview() =>
        Assert.That(Load().Checks.Select(c => (c.Name, c.Version, c.Required, c.Kind)),
            Is.EqualTo(new[]
            {
                ("no-error", 1, true, CheckKind.FixedRule), ("routed-to", 1, true, CheckKind.FixedRule),
                ("tool-used", 1, true, CheckKind.FixedRule), ("recipe-from-source", 2, true, CheckKind.FixedRule),
                ("recipe-image", 2, true, CheckKind.FixedRule), ("unpriced-items", 1, false, CheckKind.FixedRule),
                ("recipe-as-source", 3, false, CheckKind.Model),
                ("recipe-as-source.human", 2, false, CheckKind.Human)
            }));

    /// <summary>
    /// The judge is the general one, told the people asked for and what ReadRecipe gave back, and no more: it is
    /// neither told the budget nor any other tool's results.
    /// </summary>
    [Test]
    public void TheJudge_IsTold_TheServesAndWhatReadRecipeGave()
    {
        var judge = Judge();

        Assert.That(judge.ExpectedKeys, Is.EqualTo(new[] { "serves" }));
        Assert.That(judge.Sources, Is.EqualTo(new[] { "ReadRecipe" }));
    }

    /// <summary>
    /// Only ReadRecipe is named: it is called with the minion and without it, so one eval grades both runs. A search
    /// alone, or a call to the minion, is no read.
    /// </summary>
    [Test]
    public async Task ToolUsed_NamesReadRecipeOnly_ACooksCallCounts_ASearchAloneDoesNot()
    {
        var check = Load().Checks.Single(c => c.Name == "tool-used");
        var read = Trial(CookEvalMode, "recipe-bobotie",
            Turn(1, [Start(PulsRole.Cook.Wire()), SpecialistCall("ReadRecipe", "{}")]));
        var found = Trial(CookEvalMode, "recipe-bobotie",
            Turn(1,
            [
                Start(PulsRole.Cook.Wire()), SpecialistCall("FindRecipes", "{}"), SpecialistCall("StartMinion", "{}")
            ]));

        Assert.That((await check.GradeAsync(read, CancellationToken.None)).Outcome, Is.EqualTo(GradeOutcome.Pass));
        Assert.That((await check.GradeAsync(found, CancellationToken.None)).Outcome, Is.EqualTo(GradeOutcome.Fail));
    }

    /// <summary>
    /// The scenarios' keys are the ones the checks read: the route is asked by <c>routes_to</c> and the photo by
    /// <c>recipe_image</c>, which every scenario sets, so no dish is excused a photo.
    /// </summary>
    [Test]
    public void TheRouteAndThePhoto_AreAskedByTheScenariosKeys()
    {
        var checks = Load().Checks;

        Assert.That(checks.Single(c => c.Name == "recipe-image").ExpectedKeys, Is.EqualTo(new[] { "recipe_image" }));
        Assert.That(checks.Single(c => c.Name == "routed-to").ExpectedKeys, Is.EqualTo(new[] { "routes_to" }));
    }

    /// <summary>
    /// Ten dishes to work on and three held out, each expecting the cook, the people asked for and a photo, and no
    /// key a check doesn't read.
    /// </summary>
    [Test]
    public void ThereAreTenDevDishesAndThreeHeldOut_EachExpectingTheCookThePeopleAndAPhoto()
    {
        var scenarios = Load().Scenarios;

        Assert.That(scenarios.Where(s => s.Split == DatasetSplit.Dev).Select(s => s.Id), Is.EquivalentTo(Dev));
        Assert.That(scenarios.Where(s => s.Split == DatasetSplit.HeldOut).Select(s => s.Id), Is.EquivalentTo(HeldOut));
        foreach (var scenario in scenarios)
        {
            var keys = scenario.Expected.EnumerateObject().Select(p => p.Name);
            Assert.That(keys, Is.EquivalentTo(new[] { "routes_to", "serves", "recipe_image" }), scenario.Id);
            Assert.That(scenario.Expected.GetProperty("routes_to").GetString(), Is.EqualTo(PulsRole.Cook.Wire()),
                scenario.Id);
            Assert.That(scenario.Expected.GetProperty("recipe_image").GetBoolean(), Is.True, scenario.Id);
        }
    }

    /// <summary>
    /// Half the dev questions are in Afrikaans and half in English, and the held-out ones are mixed too; each says
    /// every shop will do, so the answer's work is the recipe.
    /// </summary>
    [Test]
    public void TheQuestions_AreHalfAfrikaansHalfEnglish_AndLeaveTheShopOpen()
    {
        var scenarios = Load().Scenarios;

        Assert.That(Language(scenarios, DatasetSplit.Dev, "af"), Is.EqualTo(5));
        Assert.That(Language(scenarios, DatasetSplit.Dev, "en"), Is.EqualTo(5));
        Assert.That(Language(scenarios, DatasetSplit.HeldOut, "af"), Is.GreaterThan(0));
        Assert.That(Language(scenarios, DatasetSplit.HeldOut, "en"), Is.GreaterThan(0));
        foreach (var scenario in scenarios)
            Assert.That(scenario.Input.Messages[0],
                Does.Contain("maak nie saak").Or.Contain("doesn't matter").Or.Contain("any shop will do"),
                $"{scenario.Id}: the shop");
    }

    /// <summary>
    /// Each question names the people the scenario says (read as the cook's checks read them, in words too), and
    /// asks in one message with no follow-up.
    /// </summary>
    [Test]
    public void EachQuestion_NamesItsPeople_AndAsksOnce()
    {
        foreach (var scenario in Load().Scenarios)
        {
            Assert.That(Servings.Of(scenario.Input.Messages[0]),
                Does.Contain(scenario.Expected.GetProperty("serves").GetInt32()), $"{scenario.Id}: the people");
            Assert.That(scenario.Input.Messages, Has.Count.EqualTo(1), scenario.Id);
        }
    }

    /// <summary>The held-out questions are for a release check: none is also a question to work on.</summary>
    [Test]
    public void NoHeldOutQuestion_IsAlsoADevQuestion()
    {
        var scenarios = Load().Scenarios;
        var dev = scenarios.Where(s => s.Split == DatasetSplit.Dev).SelectMany(s => s.Input.Messages).ToHashSet();

        Assert.That(scenarios.Where(s => s.Split == DatasetSplit.HeldOut).SelectMany(s => s.Input.Messages)
            .Where(dev.Contains), Is.Empty);
    }

    /// <summary>
    /// Its rubric is the judge's prompt, word for word: the hash its grades carry on every platform, so a change to
    /// the words moves it on purpose; the review asks the rubric's title as its question.
    /// </summary>
    [Test]
    public void ItsRubric_IsPinned_AndItsTitleIsTheQuestionAPersonIsAsked()
    {
        var file = Path.Combine(Folder, "Rubrics", "recipe-as-source.md");
        var rubric = PromptHeaders.Split("Rubrics/recipe-as-source.md", File.ReadAllText(file));

        Assert.That(RubricHash.Of(rubric.Body), Is.EqualTo(PinnedRubricHash));
        Assert.That(Judge().Rubric, Is.EqualTo(rubric.Body));
        Assert.That(ReviewQuestion.From(rubric.Body), Is.EqualTo("Is this recipe the page's?"));
    }

    /// <summary>
    /// A paid judge reads strictly, and failed good recipes for leaving water off the shopping list, adding salt and
    /// pepper, or ending on how to serve the dish. None of those changes the recipe, so the rubric lets them through.
    /// </summary>
    [Test]
    public void ItsRubric_DoesntFailWater_Seasoning_OrAServingLine()
    {
        string rubric = File.ReadAllText(Path.Combine(Folder, "Rubrics", "recipe-as-source.md"));

        Assert.That(rubric, Does.Contain("Water, and salt and pepper to taste, are never added or dropped"));
        Assert.That(rubric, Does.Contain("A last line on how to serve the dish"));
    }

    /// <summary>
    /// Its manual, beside its file, opens with a title and names the eval and every check, in backticks.
    /// </summary>
    [Test]
    public void ItsManual_NamesTheEvalAndEveryCheck()
    {
        var eval = Load();
        var text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "EvalFiles", eval.Manual!));

        Assert.That(text, Does.StartWith("# "));
        Assert.That(text, Does.Contain($"`{eval.Name}`"));
        Assert.That(eval.Checks.Select(check => check.Name),
            Has.All.Matches<string>(name => text.Contains($"`{name}`")));
    }

    /// <summary>
    /// Its reference transcripts pass its required checks and its known-bad ones fail them. They are written by hand,
    /// not recorded: the eval has never run.
    /// </summary>
    [Test]
    public async Task ItsReferencesAndKnownBads_Conform()
    {
        var eval = Load();
        var cases = ConformanceCases.Load(eval.Transcripts, eval.Scenarios);

        Assert.That(await EvalConformance.CheckAsync(eval, cases, CancellationToken.None), Is.Empty);
    }

    /// <summary>
    /// A dry run of the dev split on a canned client, which opens no API, calls no model and reads no page: the run
    /// lists ten conversations, the dev dishes, and no held-out one.
    /// </summary>
    [Test]
    public async Task ADevDryRun_OnACannedClient_ListsTheTenDevConversations()
    {
        var turns = Enumerable.Repeat<IReadOnlyList<SessionEvent>>([Text("Hallo")], Dev.Length).ToArray();
        var sink = new StringWriter();
        var runner = new EvalRunner(new CannedSessionClient(turns), new ProtocolWriter(sink, TimeProvider.System),
            TimeProvider.System, NullLogger<EvalRunner>.Instance);

        await runner.RunAsync(new FixedRulesOnly(Load()), new EvalRun("run_1", 1, Split: RunSplit.Dev),
            CancellationToken.None);

        var events = sink.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(EvalProtocolJson.Read).OfType<EvalEvent>().ToList();
        Assert.That(events.OfType<ConversationEvent>().Select(c => c.Task), Is.EquivalentTo(Dev));
        Assert.That(events.OfType<ModeEvent>().Single().Conversations, Is.EqualTo(Dev.Length));
    }

    /// <summary>The eval's judge, <c>recipe-as-source</c>, out of the name the file gave it.</summary>
    private static JudgeCheck Judge() =>
        (JudgeCheck)((NamedCheck)Load().Checks.Single(c => c.Name == "recipe-as-source")).Inner;

    /// <summary>The folder's copy beside these tests.</summary>
    private static string Folder { get; } = Path.Combine(AppContext.BaseDirectory, "EvalFiles", Source);

    /// <summary>How many scenarios of the split are asked in this language, by their category.</summary>
    private static int Language(IEnumerable<EvalScenario> scenarios, DatasetSplit split, string language) =>
        scenarios.Count(s => s.Split == split && s.Categories.Contains($"language:{language}"));

    /// <summary>
    /// The eval, from the folder its project copied beside these tests, with the checks the runner gathers: the
    /// general ones, each pack's checks project, and the readers of the harness and of Puls.
    /// </summary>
    private static FileEval Load() =>
        FileEval.Load(Folder, new CheckCatalog([.. GeneralChecks.All, .. WhereToBuyChecks.All, .. RecipesChecks.All]),
            new ReaderCatalog([.. GeneralReaders.All, .. PulsReaders.All]), Source);

    /// <summary>
    /// The eval with its fixed rules only, for a dry run: the judge builds its model from the environment, and a dry
    /// run must never reach one, so it is left out and the rest is the file's own.
    /// </summary>
    private sealed class FixedRulesOnly(FileEval eval) : IEval
    {
        /// <summary>The file's name.</summary>
        public string Name => eval.Name;

        /// <summary>The file's version.</summary>
        public int Version => eval.Version;

        /// <summary>What the file says it proves.</summary>
        public string Proves => eval.Proves;

        /// <summary>The file's modes.</summary>
        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions,
            CancellationToken cancellationToken) => eval.ModesAsync(sessions, cancellationToken);

        /// <summary>The file's scenarios, dev and held out.</summary>
        public IReadOnlyList<EvalScenario> Scenarios => eval.Scenarios;

        /// <summary>The file's checks that are fixed rules.</summary>
        public IReadOnlyList<ICheck> Checks => [.. eval.Checks.Where(check => check.Kind == CheckKind.FixedRule)];
    }
}
