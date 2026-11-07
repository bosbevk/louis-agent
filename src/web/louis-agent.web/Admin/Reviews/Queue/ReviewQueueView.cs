using LouisAgent.Api.Contracts.Admin.Reviews;

namespace LouisAgent.Web.Admin.Reviews.Queue;

/// <summary>
/// The Review page's model (F53.3): the answers the API handed over (the first so many of the queue), the one that
/// is open, how many were graded since the load and how many wait in all. A grade or a skip opens the next; past
/// the last one handed over nothing is open and the page reads the queue again.
/// </summary>
/// <remarks>
/// Presentation Model (Fowler): the page's state as a plain record with its own tests, placed by the components.
/// Each move is a new value, as <c>EvalListView</c> and <c>RunVerdictView</c>.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record ReviewQueueView(IReadOnlyList<ReviewQueueItem> Items, int Total, int Open, int Graded)
{
    /// <summary>The answer that is open, or none past the last one handed over.</summary>
    public ReviewQueueItem? Current => Open < Items.Count ? Items[Open] : null;

    /// <summary>How many still wait: the total the API counted, less what was graded here since.</summary>
    public int Left => Total - Graded;

    /// <summary>Every answer handed over has been opened.</summary>
    public bool Exhausted => Open >= Items.Count;

    /// <summary>The queue as the API answered it, opening on its first answer.</summary>
    /// <remarks>Fowler's <i>Replace Constructor with Factory Function</i>, the guide's <c>Record.Of</c>.</remarks>
    public static ReviewQueueView Of(ReviewQueueReply reply) => new(reply.Items, reply.Total, 0, 0);

    /// <summary>The next answer opens; a grade counts, a skip doesn't.</summary>
    public ReviewQueueView Next(bool graded) => this with { Open = Open + 1, Graded = graded ? Graded + 1 : Graded };
}
