using System.Text.Json;
using static LouisAgent.Api.Services.Benchmarks.BenchmarkDocuments;

namespace LouisAgent.Api.Services.Benchmarks;

/// <summary>
/// What a turn's tools did (F31; step 11c's first half), read from its stored stream events: every tool call Puls and
/// its specialist (or a minion) made, the ones whose result came back as an error, and why, grouped by tool and the
/// first line of the reason. Read when asked, from the events the store already keeps, so every run has it, the
/// runs before it was written too. 11b's run had every WebSearch refused by Tavily's limit and nothing on the screen
/// said so.
/// </summary>
public static class BenchmarkTools
{
    /// <summary>A tool's failures with one reason.</summary>
    public sealed record Failure(string Tool, string Reason, int Count);

    public sealed record Summary(int Calls, int Failed, IReadOnlyList<Failure> Reasons);

    public static readonly Summary None = new(0, 0, []);

    public static Summary Of(IReadOnlyList<StreamEvent> events)
    {
        var names = new Dictionary<string, string>();
        var failures = new List<(string Tool, string Reason)>();
        int calls = 0;

        void Read(string type, JsonElement data)
        {
            if (type == "sub_session")
            {
                // A specialist's event, or a minion's inside it: the same shapes, one level down.
                if (data.TryGetProperty("event", out var inner) && inner.GetString() is { } innerType && data.TryGetProperty("data", out var innerData))
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
                case "tool_result" when block.TryGetProperty("is_error", out var error) && error.ValueKind == JsonValueKind.True:
                    string tool = block.TryGetProperty("tool_use_id", out var use) && names.TryGetValue(use.GetString() ?? "", out var n) ? n : "?";
                    string reason = block.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String ? content.GetString() ?? "" : "";
                    failures.Add((tool, FirstLine(reason)));
                    break;
            }
        }

        foreach (var e in events) Read(e.Type, e.Data);
        if (calls == 0) return None;
        var reasons = failures.GroupBy(f => (f.Tool, f.Reason)).Select(g => new Failure(g.Key.Tool, g.Key.Reason, g.Count())).ToList();
        return new Summary(calls, failures.Count, reasons);
    }

    /// <summary>A reason's first line, at most 160 characters: enough to say what happened, short enough to group by.</summary>
    private static string FirstLine(string text)
    {
        string line = text.Split('\n', 2)[0].Trim();
        return line.Length <= 160 ? line : line[..159] + "…";
    }
}
