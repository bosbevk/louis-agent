using System.Text.Json;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Human;
using LouisAgent.Evals.Grading.Models;
using LouisAgent.Evals.Grading.Models.Rubrics;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Readers;
using Microsoft.Extensions.AI.Evaluation;
using static LouisAgent.Identities.Puls.Judges.Tests.JudgeTrials;

namespace LouisAgent.Identities.Puls.Judges.Tests;

/// <summary>
/// The cook's judge as an entry an eval file names, <c>meal-for-people</c>: it reads its rubric from the eval's
/// folder, and with the same bytes it is the same judge its calibration measured, under the same hash; a person's
/// review of it is built after it, asking the rubric's title.
/// </summary>
public class MealForPeopleEntryTests
{
    /// <summary>The settings a file gives: where the rubric is in the eval's folder.</summary>
    private const string Settings = """{"rubric": "rubrics/meal-for-people.md"}""";

    /// <summary>The entry reads its rubric from the eval's folder and is today's judge: name, version, keys.</summary>
    [Test]
    public void TheEntry_ReadsItsRubric_FromTheEvalsFolder()
    {
        using var folder = new RubricFolder(CookRubric.Text);

        var check = Build(Settings, folder.Root);

        Assert.That(check, Is.TypeOf<MealForPeopleCheck>());
        Assert.That((check.Name, check.Version, check.Kind, check.Required),
            Is.EqualTo(("meal-for-people", 2, CheckKind.Model, false)));
        Assert.That(check.ExpectedKeys, Is.EqualTo(new[] { "serves" }));
        Assert.That(((IRubricCheck)check).Rubric, Is.EqualTo(CookRubric.Text));
    }

    /// <summary>The rubric file's bytes are the calibrated ones, so its grades carry the same prompt hash.</summary>
    [Test]
    public async Task TheSameRubric_GradesUnderTheSameHash()
    {
        var client = new MealForPeopleTests.ScriptedJudge("""{"meal": true, "reason": "For four."}""");
        var check = MealForPeopleCheck.Create(CookRubric.Text,
            () => new ConfiguredJudge(new ChatConfiguration(client), "qwen3:8b"));

        var grade = await check.GradeAsync(
            Trial(CookEvalMode, "x", """{"serves": 4}""", null, Turn(1, [Text("Bobotie vir vier.")])),
            CancellationToken.None);

        Assert.That((grade.Outcome, grade.Judge?.PromptHash),
            Is.EqualTo((GradeOutcome.Pass, CookRubric.Hash)));
    }

    /// <summary>A rubric that isn't in the folder is refused when the eval is read, naming the file.</summary>
    [Test]
    public void AMissingRubric_IsRefused_NamingTheFile()
    {
        using var folder = new RubricFolder(null);

        var refusal = Assert.Throws<InvalidDataException>(() => Build(Settings, folder.Root));

        Assert.That(refusal!.Message, Does.Contain("meal-for-people.md"));
    }

    /// <summary>
    /// A person's review of the cook's judge, built after it, asks the rubric's title as its question.
    /// </summary>
    [Test]
    public void APersonsReview_OfTheCooksJudge_AsksItsRubricsTitle()
    {
        using var folder = new RubricFolder(CookRubric.Text);
        var judge = Build(Settings, folder.Root);

        var review = (HumanReviewCheck)new CheckCatalog([.. GeneralChecks.All, .. PulsJudges.All]).Get("human")
            .Create(Json("""{"reviews": "meal-for-people"}"""),
                new CheckContext(folder.Root, [judge], ReaderCatalog.Empty));

        Assert.That((review.Name, review.Question),
            Is.EqualTo(("meal-for-people.human", CookRubric.Question)));
    }

    /// <summary>Builds the judge the way the cook's eval file does: from Puls's judges, in this folder.</summary>
    private static ICheck Build(string settings, string folder) =>
        new CheckCatalog(PulsJudges.All).Get("meal-for-people").Create(Json(settings),
            new CheckContext(folder, [], ReaderCatalog.Empty));

    /// <summary>A JSON value from its text.</summary>
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    /// <summary>
    /// An eval folder in a temp directory, with the rubric at <c>rubrics/meal-for-people.md</c> or none.
    /// </summary>
    private sealed class RubricFolder : IDisposable
    {
        /// <summary>Where the folder is.</summary>
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "rubric-" + Guid.NewGuid().ToString("N"));

        /// <summary>Makes the folder, with the rubric's text when there is one.</summary>
        public RubricFolder(string? rubric)
        {
            Directory.CreateDirectory(Path.Combine(Root, "rubrics"));
            if (rubric is not null) File.WriteAllText(Path.Combine(Root, "rubrics", "meal-for-people.md"), rubric);
        }

        /// <summary>Removes the folder.</summary>
        public void Dispose() => Directory.Delete(Root, true);
    }
}
