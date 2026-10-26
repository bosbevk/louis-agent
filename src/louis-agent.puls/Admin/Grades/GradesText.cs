using System.Globalization;

namespace LouisAgent.Puls.Admin.Grades;

/// <summary>The grades view's words (English, as the admin is), apart from the markup's labels; nothing of Puls's own texts, so the admin can move out.</summary>
public static class GradesText
{
    public const string NoGrades = "no grades";

    /// <summary>What a grade nobody has given yet reads as (F18 step 6a): not unknown, since nobody has been asked and failed to say.</summary>
    public const string WaitingForReview = "waiting for review";

    public const string NoGraders = "This run recorded no graders.";

    /// <summary>A grader's chip: its name, and a partial score beside it ("priced-with-source 0.6"); an unknown score shows nothing.</summary>
    public static string Chip(string grader, double? score) =>
        score is > 0 and < 1 ? $"{grader} {score.Value.ToString("0.##", CultureInfo.InvariantCulture)}" : grader;

    /// <summary>A pending grade's chip: the grader and "waiting for review".</summary>
    public static string PendingChip(string grader) => $"{grader} · {WaitingForReview}";

    /// <summary>A grader's line in the run's list: its name, version and kind ("no-error v1 · FixedRule"); no version when the run recorded none.</summary>
    public static string Grader(GraderRow grader) =>
        grader.Version is { } v ? $"{grader.Name} v{v} · {grader.Kind}" : $"{grader.Name} · {grader.Kind}";
}
