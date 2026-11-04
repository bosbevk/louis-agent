using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Common.Compare.Readers;
using LouisAgent.Evals.Grading.Human;
using LouisAgent.Evals.Readers;
using LouisAgent.Identities.Puls.Evals;
using LouisAgent.Identities.Puls.Judges;
using LouisAgent.Packs.Recipes.Checks;
using LouisAgent.Packs.WhereToBuy.Checks;

namespace LouisAgent.Identities.Puls.Tests.Evals.Checks;

/// <summary>
/// The cook's eval as the runner reads it: the folder of files in Puls's evals project, copied beside these tests, with
/// its checks from the harness's general list, each pack's checks project and Puls's judges, and its readers from the
/// harness's and Puls's.
/// </summary>
internal static class CookFile
{
    /// <summary>Where the cook's folder sits in the repository.</summary>
    public const string Source = "evals/identities/louis-agent.identity.puls.evals/Evals/Cook";

    /// <summary>The copy of the folder beside these tests.</summary>
    public static string Folder { get; } = Path.Combine(AppContext.BaseDirectory, "EvalFiles", Source);

    /// <summary>The cook's eval, loaded once, from the catalogues the runner gathers.</summary>
    public static FileEval Eval { get; } =
        FileEval.Load(Folder,
            new CheckCatalog([.. GeneralChecks.All, .. WhereToBuyChecks.All, .. RecipesChecks.All, .. PulsJudges.All]),
            new ReaderCatalog([.. GeneralReaders.All, .. PulsReaders.All]), Source);

    /// <summary>
    /// One of the cook's checks as its file builds it, by the name its grades carry (<c>within-budget</c>), whatever
    /// type the file names under it.
    /// </summary>
    public static ICheck Check(string name) => Eval.Checks.Single(check => check.Name == name);

    /// <summary>The cook's judge's rubric, as its file holds it.</summary>
    public static string Rubric { get; } =
        File.ReadAllText(Path.Combine(Folder, "Rubrics", "meal-for-people.md"));

    /// <summary>What a person reviewing the cook's judge is asked: the rubric's title as a question.</summary>
    public static string Question { get; } = ReviewQuestion.From(Rubric);

    /// <summary>
    /// The rubric's hash, the one the judge's grades carry on every platform (<c>RubricHash</c>: line endings made LF).
    /// </summary>
    public const string RubricHash = "51f8ca5a5f84";
}
