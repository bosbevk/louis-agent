using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Reviews;
using LouisAgent.Web.Api;

namespace LouisAgent.Web.Evals.Admin.Reviews.Queue;

/// <summary>
/// How many answers wait for this reviewer's grade (F53.3): read once when the portal opens, set by the Review page
/// as it loads and after each grade, shown by the badge beside Review in the sidebar. Null until anyone has read it.
/// </summary>
/// <remarks>
/// Observer, as <c>ReviewerState</c> and <c>RunNotesState</c>: a scoped service the badge and the page share; one
/// sets the count, the other draws it, and neither knows the other.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
public sealed class ReviewQueueState
{
    /// <summary>How many wait, or null before the first read.</summary>
    public int? Count { get; private set; }

    /// <summary>Raised when the count changes, so the badge draws again.</summary>
    public event Action? Changed;

    /// <summary>The count as the API or the Review page last knew it.</summary>
    public void Set(int count)
    {
        if (Count == count) return;
        Count = count;
        Changed?.Invoke();
    }

    /// <summary>
    /// Reads the queue's total for the kept name once, if nobody has: the sidebar's badge and the admin home's card
    /// (F144) both ask, and the first one reads it. An API that refuses (no key yet, no store) leaves it unread.
    /// </summary>
    public async Task ReadOnceAsync(AgentApi api, ReviewerState reviewer)
    {
        await reviewer.LoadAsync();
        if (Count is not null) return;
        try
        {
            string who = reviewer.Name is { } name ? $"reviewer={Uri.EscapeDataString(name)}&" : "";
            var reply = await api.GetAsync<ReviewQueueReply>($"v1/admin/evals/reviews/queue?{who}limit=1",
                AdminJson.SnakeCase);
            Set(reply.Total);
        }
        catch (Exception ex) when (ex is AgentApiException or HttpRequestException)
        {
            // No key yet, or no store: the count stays unread until the Review page sets one.
        }
    }
}
