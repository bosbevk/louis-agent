using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Benchmarks.Reviews;

/// <summary>
/// Which of people's grades count (F18 step 6a.2). <see cref="Latest"/>: of a reviewer's grades of one conversation for one grader, the latest, the earlier ones being the history (a name is the same name whatever its case, and the store keeps it as typed).
/// <see cref="Decided"/>: of those, the pass or fail that stands for the conversation: a reviewer's later unsure takes back their earlier pass or fail (they are no longer sure), and
/// across reviewers the latest decided one wins. These are what the agreement counts and the label export writes.
/// </summary>
public static class HumanGrades
{
    private sealed record Slot(ConversationKey Conversation, string Grader);

    public static IReadOnlyList<HumanGrade> Latest(IEnumerable<HumanGrade> grades) =>
    [
        .. grades.GroupBy(g => new Slot(g.Conversation, g.Grader))
            .SelectMany(slot =>
                slot.GroupBy(g => g.Reviewer, StringComparer.OrdinalIgnoreCase)
                    .Select(reviewer => reviewer.OrderBy(g => g.At).Last()))
            .OrderBy(g => g.At)
    ];

    public static IReadOnlyList<HumanGrade> Decided(IEnumerable<HumanGrade> grades) =>
    [
        .. Latest(grades).Where(g => g.Verdict != HumanVerdict.Unsure).GroupBy(g => new Slot(g.Conversation, g.Grader))
            .Select(group => group.OrderBy(g => g.At).Last()).OrderBy(g => g.At)
    ];
}
