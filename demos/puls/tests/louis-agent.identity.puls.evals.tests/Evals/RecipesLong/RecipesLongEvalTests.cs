using LouisAgent.Choices;
using LouisAgent.Evals;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Conformance;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Common.Compare.Readers;
using LouisAgent.Evals.Readers;
using LouisAgent.Identities.Puls.Evals;
using LouisAgent.Identities.Puls.Evals.Checks.Cook.Scoring;
using LouisAgent.Identities.Puls.Evals.Checks.Shared;
using LouisAgent.Packs.Recipes.Checks;
using LouisAgent.Packs.WhereToBuy.Checks;

namespace LouisAgent.Identities.Puls.Tests.Evals.RecipesLong;

/// <summary>
/// Puls's long recipes eval: thirty dishes the recipes eval doesn't ask, baking among them, graded the same way. Ten
/// are made whole and name no people, so they also show Puls no longer asks how many for a cake.
/// </summary>
public class RecipesLongEvalTests
{
    /// <summary>Where the folder sits in the repository.</summary>
    private const string Source = "demos/puls/evals/louis-agent.identity.puls.evals/Evals/RecipesLong";

    /// <summary>What a question that names no people has under <c>serves</c>: the judge is still asked.</summary>
    private const string Whole = "as the page makes it";

    /// <summary>It is the identity's eval, under its dataset's name, at its first version.</summary>
    [Test]
    public void ItIsPulssLongRecipesEval_UnderItsDatasetsName()
    {
        var eval = Load();

        Assert.That((eval.Name, eval.Dataset.Name, eval.Identity, eval.Version, eval.Dataset.Version),
            Is.EqualTo(("puls-recipes-long", "puls-recipes-long", PulsIdentityName.Puls.Wire(), 1, 1)));
        Assert.That(eval.Manual, Is.EqualTo($"{Source}/manual.md"));
    }

    /// <summary>Thirty questions to work on, half in each language, none held out, the shop left open.</summary>
    [Test]
    public void ThereAreThirtyQuestions_HalfAfrikaansHalfEnglish()
    {
        var scenarios = Load().Scenarios;

        Assert.That(scenarios.Where(s => s.Split == DatasetSplit.Dev).ToList(), Has.Count.EqualTo(30));
        Assert.That(scenarios.Count(s => s.Categories.Contains("language:af")), Is.EqualTo(15));
        Assert.That(scenarios.Count(s => s.Categories.Contains("language:en")), Is.EqualTo(15));
        Assert.That(scenarios.Count(s => s.Categories.Contains("kind:baking")), Is.EqualTo(15));
        foreach (var scenario in scenarios)
            Assert.That(scenario.Input.Messages[0],
                Does.Contain("maak nie saak").Or.Contain("doesn't matter").Or.Contain("any shop will do"),
                $"{scenario.Id}: the shop");
    }

    /// <summary>
    /// Ten questions say no number, the rest name the number their scenario says. A missing key would skip the judge,
    /// so a question with no number still sets <c>serves</c>, in words.
    /// </summary>
    [Test]
    public void AQuestion_NamesItsPeople_OrSaysNoNumberAtAll()
    {
        var scenarios = Load().Scenarios;
        var whole = scenarios.Where(s => s.Expected.GetProperty("serves").ToString() == Whole).ToList();

        Assert.That(whole, Has.Count.EqualTo(10));
        foreach (var scenario in whole) Assert.That(Servings.Of(scenario.Input.Messages[0]), Is.Empty, scenario.Id);
        foreach (var scenario in scenarios.Except(whole))
            Assert.That(Servings.Of(scenario.Input.Messages[0]),
                Does.Contain(scenario.Expected.GetProperty("serves").GetInt32()), scenario.Id);
    }

    /// <summary>The rubric is the recipes eval's, plus one line on what a question with no number means.</summary>
    [Test]
    public void ItsRubric_IsTheRecipesEvals_PlusTheLineForNoNumber()
    {
        string line = $"When `serves` says `{Whole}`, nobody asked for a number, so the factor is 1 and every amount is " +
                      "the print's. ";
        string other = Path.Combine(AppContext.BaseDirectory, "EvalFiles",
            "demos/puls/evals/louis-agent.identity.puls.evals/Evals/Recipes/Rubrics/recipe-as-source.md");

        string rubric = File.ReadAllText(Path.Combine(Folder, "Rubrics", "recipe-as-source.md"));

        Assert.That(rubric, Does.Contain(line));
        Assert.That(rubric.Replace(line, "").ReplaceLineEndings(), Is.EqualTo(File.ReadAllText(other).ReplaceLineEndings()));
    }

    /// <summary>Its references and known-bads hold up against its checks.</summary>
    [Test]
    public async Task ItsReferencesAndKnownBads_Conform()
    {
        var eval = Load();
        var cases = ConformanceCases.Load(eval.Transcripts, eval.Scenarios);

        Assert.That(await EvalConformance.CheckAsync(eval, cases, CancellationToken.None), Is.Empty);
    }

    /// <summary>The folder's copy beside these tests.</summary>
    private static string Folder { get; } = Path.Combine(AppContext.BaseDirectory, "EvalFiles", Source);

    /// <summary>The eval, with the checks and readers the runner gathers.</summary>
    private static FileEval Load() =>
        FileEval.Load(Folder, new CheckCatalog([.. GeneralChecks.All, .. WhereToBuyChecks.All, .. RecipesChecks.All]),
            new ReaderCatalog([.. GeneralReaders.All, .. PulsReaders.All]), Source);
}
