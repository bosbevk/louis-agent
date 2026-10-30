using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Human;
using LouisAgent.Identities.Puls.Evals.Checks.Cook.Grading;
using LouisAgent.Identities.Puls.Judges;

namespace LouisAgent.Identities.Puls.Tests.Evals.Checks;

/// <summary>
/// The cook's eval as the runner reads it: the folder of files beside the identity, copied beside these tests, with
/// its checks from the harness's general list, Puls's fixed rules and Puls's judges.
/// </summary>
internal static class CookFile
{
    /// <summary>Where the cook's folder sits in the repository.</summary>
    public const string Source = "identities/louis-agent.identity.puls/Evals/Cook";

    /// <summary>The copy of the folder beside these tests.</summary>
    public static string Folder { get; } = Path.Combine(AppContext.BaseDirectory, "EvalFiles", Source);

    /// <summary>The cook's eval, loaded once.</summary>
    public static FileEval Eval { get; } =
        FileEval.Load(Folder, new CheckCatalog([.. GeneralChecks.All, .. PulsChecks.All, .. PulsJudges.All]), Source);

    /// <summary>The cook's judge's rubric, as its file holds it.</summary>
    public static string Rubric { get; } =
        File.ReadAllText(Path.Combine(Folder, "Rubrics", "meal-for-people.md"));

    /// <summary>What a person reviewing the cook's judge is asked: the rubric's title as a question.</summary>
    public static string Question { get; } = ReviewQuestion.From(Rubric);

    /// <summary>The rubric's hash, the one the judge's grades carry and its calibration measured.</summary>
    public const string RubricHash = "161e5cebcc15";
}
