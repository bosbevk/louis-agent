using LouisAgent.Core.Skills.Headers;
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
        "identities/louis-agent.identity.puls.evals/Evals/Cook");

    /// <summary>The rubric as the judge reads it: its file's body, after the header (CL27).</summary>
    public static string Text { get; } = PromptHeaders.Split("Rubrics/meal-for-people.md",
        File.ReadAllText(Path.Combine(Folder, "Rubrics", "meal-for-people.md"))).Body;

    /// <summary>What a person reviewing the judge is asked: the rubric's title as a question.</summary>
    public static string Question { get; } = ReviewQuestion.From(Text);

    /// <summary>
    /// The rubric's hash, the one the judge's grades carry on every platform (<c>RubricHash</c>: line endings made LF).
    /// The runs before F49.2 carried <c>161e5cebcc15</c>, the same words read with CRLF on Windows; the runs before the
    /// recalibration of 2026-11-03 carried <c>438a8deb91f4</c>, then <c>2ab944be4e29</c>: the rubric before it became
    /// an ordered decision, and before an earlier turn's recipe counted in its first question.
    /// </summary>
    public const string Hash = "51f8ca5a5f84";
}
