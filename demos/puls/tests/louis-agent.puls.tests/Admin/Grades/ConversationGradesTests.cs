using LouisAgent.Puls.Admin.Grades;
using Contract = LouisAgent.Api.Contracts.Admin.Runs;

namespace LouisAgent.Puls.Tests.Admin.Grades;

/// <summary>A conversation's grades and a run's graders, read from the contract's records into the portal's.</summary>
public class ConversationGradesTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

    /// <summary>The grades in order with their outcomes read, the outcome, and the people's grades.</summary>
    [Test]
    public void AConversationsGrades_AreReadFromTheContract()
    {
        var row = Row([
                new Contract.GradeRow("no-error", 1, 1, "Pass", "fine", null, null),
                new Contract.GradeRow("meal.human", 2, 0, "pending", "waits", null, null)
            ], "Fail",
            [
                new Contract.HumanGradeRow(SampleMode.Alpha, 1, "bobotie", "meal.human", "meal", "pass", "good", "ann",
                    At, true)
            ]);

        var grades = ConversationGrades.Of(row);

        Assert.That(grades.Grades,
            Is.EqualTo(new[]
            {
                new GradeRow("no-error", 1, 1, GradeOutcome.Pass, "fine"),
                new GradeRow("meal.human", 2, 0, GradeOutcome.Pending, "waits")
            }));
        Assert.That(grades.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grades.Human,
            Is.EqualTo(new[] { new HumanGradeRow("meal.human", "meal", "pass", "good", "ann", At) }));
    }

    /// <summary>A conversation from before the graders has no grades, no outcome and no people's grades.</summary>
    [Test]
    public void AConversationFromBefore_HasNone()
    {
        var grades = ConversationGrades.Of(Row(null, null, null!));

        Assert.That((grades.Grades, grades.Outcome, grades.HumanGrades), Is.EqualTo(((IReadOnlyList<GradeRow>?)null,
            (GradeOutcome?)null, (IReadOnlyList<HumanGradeRow>?)null)));
    }

    /// <summary>A grader keeps its name, version, kind and question.</summary>
    [Test]
    public void AGrader_IsReadFromTheContract() =>
        Assert.That(GraderRow.Of(new Contract.RunGraderRow("meal.human", 2, "Human", "A meal?")),
            Is.EqualTo(new GraderRow("meal.human", 2, "Human", "A meal?")));

    /// <summary>A conversation row with these grades, outcome and people's grades.</summary>
    private static Contract.ConversationRow Row(IReadOnlyList<Contract.GradeRow>? grades, string? outcome,
        IReadOnlyList<Contract.HumanGradeRow> human) =>
        new(SampleMode.Alpha, 1, "bobotie", 0, null, 0, 1, grades, outcome, human);
}
