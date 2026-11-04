using LouisAgent.Api.Contracts.Admin.Reviews;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Api.Evals.Reviews.Queue;
using LouisAgent.Choices;

namespace LouisAgent.Api.Admin.Replies;

/// <summary>Builds the review queue's rows from what the store kept.</summary>
/// <remarks>
/// Replace Constructor with Factory Function (Fowler), as C# 14 static extension members, as the other replies: the
/// contract's records stay plain and the API knows how to fill them.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public static class ReviewQueueReplies
{
    /// <summary>A queue item is where the answer is from, with the rows the review panel reads.</summary>
    extension(ReviewQueueItem)
    {
        /// <summary>
        /// The row for one waiting answer: the run's graders, the conversation, the turns as asked and answered.
        /// </summary>
        public static ReviewQueueItem Of(QueueItem item) => new(
            item.Run.Id,
            item.Run.Eval ?? item.Run.Check,
            item.Run.StartedAt,
            [.. (item.Run.Graders ?? []).Select(g => new RunGraderRow(g.Name, g.Version, g.Kind.Wire(), g.Question))],
            ConversationRow.Of(item.Conversation, item.Human),
            [.. item.Turns.Select(t => t.Question)],
            item.Turns.Count > 0 ? item.Turns[^1].Answer : null);
    }
}
