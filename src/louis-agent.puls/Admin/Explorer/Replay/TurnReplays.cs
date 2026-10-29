using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.SessionStream;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Chats;

namespace LouisAgent.Puls.Admin.Explorer.Replay;

/// <summary>
/// Plays a stored turn again: fetches its events from the store, then folds them into a turn through the chat's own
/// stream, at the events' own pace, so the explorer shows the answer typing out as it did.
/// </summary>
/// <remarks>
/// The turn and the chats side by side both replay this way, so the fetch and the pace live here once. The events
/// are read as the contract's <c>TurnEventsReply</c>.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public static class TurnReplays
{
    /// <summary>A turn's stored events; none when the store can't give them, so that mode just doesn't play.</summary>
    public static async Task<IReadOnlyList<TurnEventRow>> FetchAsync(AgentApi api, string runId, string mode,
        int repeat, string task, int turn)
    {
        // The mode and the task go into the address, so they are escaped.
        string turnUrl = $"{Uri.EscapeDataString(mode)}/{repeat}/{Uri.EscapeDataString(task)}/{turn}";
        try
        {
            var reply = await api.GetAsync<TurnEventsReply?>(
                $"v1/admin/benchmarks/runs/{runId}/turns/{turnUrl}/events", AdminJson.SnakeCase);
            return reply?.Events ?? [];
        }
        // A turn whose events can't be fetched plays nothing.
        catch (AgentApiException)
        {
            return [];
        }
    }

    /// <summary>
    /// Plays the events into a turn at their own rhythm, three times faster, and asks for a redraw every third event
    /// or after any pause.
    /// </summary>
    public static async Task PlayAsync(Turn turn, IReadOnlyList<TurnEventRow> events, Action redraw,
        CancellationToken stop)
    {
        DateTimeOffset? last = events[0].At;
        int since = 0;
        foreach (var e in events)
        {
            int wait = ReplayPace.WaitMs(e.At, last);
            last = e.At ?? last;
            if (wait > 0) await Task.Delay(wait, stop);
            TurnStream.Apply(turn, SessionEvent.From(e.Type, e.Data));
            if (++since % 3 != 0 && wait == 0) continue;

            redraw();
            since = 0;
        }
    }
}
