using LouisAgent.Evals.Grading.Human;

namespace LouisAgent.Identities.Puls.Judges.Tests;

/// <summary>
/// The cook's judge's rubric as the runner reads it: the file in the cook's folder beside the identity, copied beside
/// these tests.
/// </summary>
internal static class CookRubric
{
    /// <summary>The copy of the cook's folder beside these tests.</summary>
    public static string Folder { get; } = Path.Combine(AppContext.BaseDirectory, "EvalFiles",
        "identities/louis-agent.identity.puls/Evals/Cook");

    /// <summary>The rubric, as its file holds it.</summary>
    public static string Text { get; } = File.ReadAllText(Path.Combine(Folder, "Rubrics", "meal-for-people.md"));

    /// <summary>What a person reviewing the judge is asked: the rubric's title as a question.</summary>
    public static string Question { get; } = ReviewQuestion.From(Text);

    /// <summary>The rubric's hash, the one the judge's grades carry and its calibration measured.</summary>
    public const string Hash = "161e5cebcc15";
}
