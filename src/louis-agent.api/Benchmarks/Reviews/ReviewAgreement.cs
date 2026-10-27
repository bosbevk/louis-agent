using LouisAgent.Api.Benchmarks.Runs;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Benchmarks.Reviews;

/// <summary>
/// How often a judge agreed with the people, as counts (F18 step 6a.2): the person's pass or fail against the judge's pass, fail or can't tell on the same conversation. Counts only: the
/// API reads, it doesn't rate (5a); the share, and what is enough, are the portal's and Louis's to read.
/// </summary>
public sealed record AgreementTally(
    int PersonPassJudgePass,
    int PersonPassJudgeFail,
    int PersonPassJudgeUnknown,
    int PersonFailJudgePass,
    int PersonFailJudgeFail,
    int PersonFailJudgeUnknown);

public static class ReviewAgreement
{
    /// <summary>The tally for <paramref name="modelGrader"/> over the decided grades of its conversations; a grade of a conversation the run doesn't have is left out, and a judge that gave no grade is unknown.</summary>
    public static AgreementTally Of(IReadOnlyList<Conversation> conversations, IReadOnlyList<HumanGrade> decided,
        string modelGrader)
    {
        int[] counts = new int[6];
        foreach (var grade in decided.Where(g => g.Grader == modelGrader))
        {
            if (conversations.FirstOrDefault(c => c.Key == grade.Conversation) is not { } conversation) continue;
            var judge = conversation.Grades?.FirstOrDefault(g => g.Grader == modelGrader)?.Outcome;
            int column = judge switch { BenchmarkOutcome.Pass => 0, BenchmarkOutcome.Fail => 1, _ => 2 };
            counts[(grade.Verdict == HumanVerdict.Pass ? 0 : 3) + column]++;
        }

        return new AgreementTally(counts[0], counts[1], counts[2], counts[3], counts[4], counts[5]);
    }
}
