using LouisAgent.Api.Evals.Reviews.Store;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Evals.Protocol.Kinds;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Evals.Reviews.Queue;

/// <summary>
/// The answers waiting for a person (F53.3): over the newest stored runs, every conversation whose human review is
/// still pending and that this reviewer hasn't answered; with no reviewer, that nobody has. The same rule as the
/// explorer's "to review" (<c>ReviewStatus</c> in the portal), kept here so one route serves every page.
/// </summary>
/// <remarks>
/// The walk fetches each run's conversations and people's grades once, and the turns only for the items it hands
/// back (the first <c>take</c>): the total is a count, not a read of every transcript.
/// </remarks>
public static class ReviewQueue
{
    /// <summary>How many runs back the queue looks: the store's newest, as the list of runs shows them.</summary>
    private const int Runs = 50;

    /// <summary>
    /// The first <paramref name="take"/> answers waiting for <paramref name="reviewer"/> (nobody when null), newest
    /// run first, and how many wait in all.
    /// </summary>
    public static async Task<(IReadOnlyList<QueueItem> Items, int Total)> ListAsync(IEvalStore store,
        IHumanReviewStore reviews, string? reviewer, int take, CancellationToken cancellationToken)
    {
        var items = new List<QueueItem>();
        int total = 0;
        foreach (var run in await store.ListRunsAsync(limit: Runs, cancellationToken: cancellationToken))
        {
            var humans = ReviewQueries.HumanReviewsOf(run);
            if (humans.Count == 0) continue;

            var latest = HumanGrades.Latest(await reviews.OfRunAsync(run.Id, cancellationToken));
            foreach (var c in await store.ConversationsAsync(run.Id, cancellationToken))
            {
                var own = latest.Where(g => g.Conversation == c.Key).ToList();
                if (!Waits(c, humans, own, reviewer)) continue;

                total++;
                if (items.Count >= take) continue;
                var turns = (await store.TurnsAsync(run.Id, c.Key, cancellationToken)).OrderBy(t => t.Key.Turn)
                    .ToList();
                items.Add(new QueueItem(run, c, own, turns));
            }
        }

        return (items, total);
    }

    /// <summary>
    /// A conversation waits when one of the run's human reviews is still pending on it and the reviewer (or, with
    /// none named, anyone) hasn't graded that review's judge.
    /// </summary>
    private static bool Waits(Conversation c, HashSet<string> humans, IReadOnlyList<HumanGrade> own,
        string? reviewer) =>
        (c.Grades ?? []).Any(g => humans.Contains(g.Grader) && g.Outcome == GradeOutcome.Pending &&
                                  !Answered(own, g.Grader[..^HumanReview.Suffix.Length], reviewer));

    /// <summary>Whether the reviewer (anyone, with none named) has a grade of this judge on the conversation.</summary>
    private static bool Answered(IReadOnlyList<HumanGrade> own, string judge, string? reviewer) =>
        own.Any(g => g.Grader == judge &&
                     (reviewer is null || string.Equals(g.Reviewer, reviewer, StringComparison.OrdinalIgnoreCase)));
}
