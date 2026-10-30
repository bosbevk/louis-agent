using LouisAgent.Api.Evals.Reviews.Store;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Api.Evals.Store;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Evals.Reviews;

/// <summary>
/// What the people's grades of a run add up to: the human reviews the run has, and a judge's labels (each decided
/// grade of it with its conversation's turns). The label zip, the judge check and the run's read all ask here.
/// </summary>
internal static class ReviewQueries
{
    /// <summary>The names of the human reviews a run has.</summary>
    public static HashSet<string> HumanReviewsOf(Run run) =>
        (run.Graders ?? [])
        .Where(g => g.Kind == CheckKind.Human &&
                    g.Name.EndsWith(HumanReview.Suffix, StringComparison.Ordinal))
        .Select(g => g.Name)
        .ToHashSet();

    /// <summary>
    /// A judge's labels in a run: each decided grade of it with its conversation's turns in order. A conversation with
    /// no turns stored labels nothing.
    /// </summary>
    public static async Task<List<(HumanGrade Grade, IReadOnlyList<Turn> Turns)>> DecidedLabelsAsync(
        IEvalStore store, IHumanReviewStore reviews, string id, string grader,
        CancellationToken cancellationToken)
    {
        var labels = new List<(HumanGrade, IReadOnlyList<Turn>)>();
        var decided = HumanGrades.Decided(await reviews.OfRunAsync(id, cancellationToken))
            .Where(g => g.Grader == grader);
        foreach (var grade in decided)
        {
            var turns = (await store.TurnsAsync(id, grade.Conversation, cancellationToken))
                .OrderBy(t => t.Key.Turn)
                .ToList();
            if (turns.Count > 0) labels.Add((grade, turns));
        }

        return labels;
    }
}
