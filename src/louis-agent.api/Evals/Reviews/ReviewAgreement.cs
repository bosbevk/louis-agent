using LouisAgent.Api.Evals.Runs.Kinds;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Evals.Reviews;

/// <summary>
/// How often a judge agreed with the people, as plain counts: the person's pass or fail against the judge's pass,
/// fail or can't tell on the same conversation.
/// </summary>
/// <remarks>
/// Counts only, because the API reads and doesn't rate: the share, and what counts as enough, are for the portal and
/// Louis to judge.
/// </remarks>
public sealed record AgreementTally(
    int PersonPassJudgePass,
    int PersonPassJudgeFail,
    int PersonPassJudgeUnknown,
    int PersonFailJudgePass,
    int PersonFailJudgeFail,
    int PersonFailJudgeUnknown);

/// <summary>Counts how a judge's grades line up with the people's.</summary>
public static class ReviewAgreement
{
    /// <summary>
    /// The tally for one model grader over the people's decided grades. A grade of a conversation the run doesn't have
    /// is left out, and a judge that gave no grade counts as can't tell.
    /// </summary>
    public static AgreementTally Of(IReadOnlyList<Conversation> conversations, IReadOnlyList<HumanGrade> decided,
        string modelGrader)
    {
        int[] counts = new int[6];
        foreach (var grade in decided.Where(g => g.Grader == modelGrader))
        {
            if (conversations.FirstOrDefault(c => c.Key == grade.Conversation) is not { } conversation) continue;

            // The six counts are a 2 by 3 table: the person's verdict picks the row, the judge's answer the column.
            var judge = conversation.Grades?.FirstOrDefault(g => g.Grader == modelGrader)?.Outcome;
            int column = judge switch { EvalOutcome.Pass => 0, EvalOutcome.Fail => 1, _ => 2 };
            counts[(grade.Verdict == HumanVerdict.Pass ? 0 : 3) + column]++;
        }

        return new AgreementTally(counts[0], counts[1], counts[2], counts[3], counts[4], counts[5]);
    }
}
