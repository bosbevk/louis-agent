using LouisAgent.Api.Benchmarks.Store;
using System.Text.Json;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Benchmarks.Reports;

/// <summary>
/// Counts what a turn's tools did, from its saved stream events: every call Puls, its specialist or a minion made,
/// which ones failed, and why, grouped by tool and the first line of the reason. Without it, a run where every web
/// search was refused looked fine on screen.
/// </summary>
/// <remarks>
/// The counts are kept on the turn when it's saved, since reading every turn's events made each run and trend slower
/// than the last (review of PR #55). A turn saved before that is counted once and written back.
/// </remarks>
public static class BenchmarkTools
{
    /// <summary>How often one tool failed for one reason.</summary>
    public sealed record Failure(string Tool, string Reason, int Count);

    /// <summary>A turn's tool calls, how many failed, and the reasons.</summary>
    public sealed record Summary(int Calls, int Failed, IReadOnlyList<Failure> Reasons);

    /// <summary>A turn that called no tools.</summary>
    public static readonly Summary None = new(0, 0, []);

    /// <summary>Counts the tool calls and failures in a turn's events, the sub-sessions' ones included.</summary>
    public static Summary Of(IReadOnlyList<StreamEvent> events)
    {
        var names = new Dictionary<string, string>();
        var failures = new List<(string Tool, string Reason)>();
        int calls = 0;

        // Reads one event: a call is counted and its name kept by id, so a failed result can say which tool it was.
        void Read(string type, JsonElement data)
        {
            if (type == "sub_session")
            {
                // A specialist's event, or a minion's inside it: the same shapes, one level down.
                if (data.TryGetProperty("event", out var inner) && inner.GetString() is { } innerType &&
                    data.TryGetProperty("data", out var innerData))
                    Read(innerType, innerData);
                return;
            }

            if (type != "content_block_start" || !data.TryGetProperty("content_block", out var block)) return;
            switch (block.TryGetProperty("type", out var t) ? t.GetString() : null)
            {
                case "tool_use":
                    calls++;
                    if (block.TryGetProperty("id", out var id) && block.TryGetProperty("name", out var name))
                        names[id.GetString() ?? ""] = name.GetString() ?? "?";
                    break;
                case "tool_result" when block.TryGetProperty("is_error", out var error) &&
                                        error.ValueKind == JsonValueKind.True:
                    string tool = block.TryGetProperty("tool_use_id", out var use) &&
                                  names.TryGetValue(use.GetString() ?? "", out var n)
                        ? n
                        : "?";
                    string reason =
                        block.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String
                            ? content.GetString() ?? ""
                            : "";
                    failures.Add((tool, FirstLine(reason)));
                    break;
            }
        }

        foreach (var e in events) Read(e.Type, e.Data);
        if (calls == 0) return None;

        // One line per tool and reason, with how often it happened.
        var reasons = failures.GroupBy(f => (f.Tool, f.Reason))
            .Select(g => new Failure(g.Key.Tool, g.Key.Reason, g.Count())).ToList();
        return new Summary(calls, failures.Count, reasons);
    }

    /// <summary>
    /// Gives a turn's tool counts. An older turn without them is counted from its events once and saved, so the next
    /// read is quick.
    /// </summary>
    public static async Task<Summary> OfTurnAsync(IBenchmarkStore store, string run, Turn turn,
        CancellationToken cancellationToken = default)
    {
        if (turn.Tools is { } kept) return kept;
        var events = await store.TurnEventsAsync(run, turn.Key, cancellationToken);
        var counted = Of(events);
        await store.SaveTurnAsync(run, turn with { Tools = counted }, events, cancellationToken);
        return counted;
    }

    /// <summary>
    /// Cuts a reason to its first line, at most 160 characters: enough to say what happened, short enough to group by.
    /// </summary>
    private static string FirstLine(string text)
    {
        string line = text.Split('\n', 2)[0].Trim();
        return line.Length <= 160 ? line : line[..159] + "…";
    }
}
