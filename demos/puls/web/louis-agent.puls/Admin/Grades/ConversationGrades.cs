using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Grades.Reading;
using Contract = LouisAgent.Api.Contracts.Admin.Runs;

namespace LouisAgent.Puls.Admin.Grades;

/// <summary>
/// A conversation's grades in the stored order, its outcome, and the people's grades of it (each reviewer's latest).
/// A conversation from before graders were kept has none, and one nobody graded, or from an API that doesn't send
/// them, has no people's grades.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public sealed record ConversationGrades(
    IReadOnlyList<GradeRow>? Grades,
    GradeOutcome? Outcome,
    IReadOnlyList<HumanGradeRow>? HumanGrades = null)
{
    /// <summary>No grades at all: a conversation from before the graders.</summary>
    public static readonly ConversationGrades None = new(null, null);

    /// <summary>The people's grades, or none.</summary>
    public IReadOnlyList<HumanGradeRow> Human => HumanGrades ?? [];

    /// <summary>A conversation's grades as the stored run sent them, the outcomes in the portal's words.</summary>
    /// <remarks>
    /// Fowler's <i>Replace Constructor with Factory Function</i>, the guide's <c>Record.Of</c>: the explorer reads the
    /// contract's row, and the review pill, the chips and the panel keep the shape they were written for.
    /// </remarks>
    public static ConversationGrades Of(ConversationRow row) =>
        new(row.Grades?.Select(Grade).ToList(), row.Outcome is { } word ? GradeOutcomes.Parse(word) : null,
            row.HumanGrades?.Select(Person).ToList());

    /// <summary>One grader's grade, its outcome in the portal's words.</summary>
    private static GradeRow Grade(Contract.GradeRow g) =>
        new(g.Grader, g.Version, g.Score, GradeOutcomes.Parse(g.Outcome), g.Reason ?? "");

    /// <summary>One person's grade, as the review pill and panel read it.</summary>
    private static HumanGradeRow Person(Contract.HumanGradeRow h) =>
        new(h.Grader ?? "", h.ModelGrader ?? "", h.Verdict ?? "", h.Note ?? "", h.Reviewer ?? "", h.At);
}
