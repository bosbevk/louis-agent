using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Benchmarks.Reviews;

/// <summary>
/// Works out which of people's grades count. These are what the agreement counts and the label export writes.
/// </summary>
public static class HumanGrades
{
    /// <summary>One conversation as graded for one model grader: the place a person's grades pile up.</summary>
    private sealed record Slot(ConversationKey Conversation, string Grader);

    /// <summary>
    /// Each reviewer's latest grade of a conversation for a grader; the earlier ones are history. A name counts as the
    /// same name whatever its case, while the store keeps it as typed.
    /// </summary>
    public static IReadOnlyList<HumanGrade> Latest(IEnumerable<HumanGrade> grades) =>
    [
        .. grades.GroupBy(g => new Slot(g.Conversation, g.Grader))
            .SelectMany(slot =>
                slot.GroupBy(g => g.Reviewer, StringComparer.OrdinalIgnoreCase)
                    .Select(reviewer => reviewer.OrderBy(g => g.At).Last()))
            .OrderBy(g => g.At)
    ];

    /// <summary>
    /// The pass or fail that stands for each conversation. A reviewer's later unsure takes back their earlier pass or
    /// fail (they're no longer sure), and across reviewers the latest decided grade wins.
    /// </summary>
    public static IReadOnlyList<HumanGrade> Decided(IEnumerable<HumanGrade> grades) =>
    [
        .. Latest(grades).Where(g => g.Verdict != HumanVerdict.Unsure).GroupBy(g => new Slot(g.Conversation, g.Grader))
            .Select(group => group.OrderBy(g => g.At).Last()).OrderBy(g => g.At)
    ];
}
