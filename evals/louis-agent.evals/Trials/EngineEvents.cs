using System.Text.Json;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Trials;

/// <summary>
/// The one reader of the engine's events for the common graders (F18 step 4.2): whether a turn raised an error, what it cost,
/// and which role its first sub-session went to. It reads the stream as the API sends it: the cost is the usage the stream
/// reports on message_delta (or on error), which is the ledger's total for the turn, and no price is worked out here.
/// </summary>
public static class EngineEvents
{
    /// <summary>True when the turn streamed an error event, or ended in a failure.</summary>
    public static bool RaisedError(TurnTranscript turn) => turn.Failure is not null || turn.Events.Any(e => e is ErrorReported);

    /// <summary>The turn's cost in the ledger's currency; null when unknown (no usage, no cost, or a request with no price).</summary>
    public static decimal? CostOf(TurnTranscript turn)
    {
        // The usage is on the turn's last message_delta, or on its error when it failed: the stream's end.
        var end = turn.Events.LastOrDefault(e => e is MessageDelta or ErrorReported);
        if (end is null || !end.Data.TryGetProperty("usage", out var usage) || usage.ValueKind != JsonValueKind.Object) return null;
        if (usage.TryGetProperty("unpriced", out var unpriced) && unpriced.GetInt32() > 0) return null;
        return usage.TryGetProperty("cost", out var cost) && cost.ValueKind == JsonValueKind.Number ? cost.GetDecimal() : null;
    }

    /// <summary>The role of the turn's first StartSubSession call, or null when the agent answered itself.</summary>
    public static string? RoutedRole(TurnTranscript turn)
    {
        foreach (var e in turn.Events)
        {
            if (e is not ContentBlockStarted || !e.Data.TryGetProperty("content_block", out var block)) continue;
            if (block.GetProperty("type").GetString() == "tool_use" && block.GetProperty("name").GetString() == StartSubSession
                && block.TryGetProperty("input", out var input) && input.TryGetProperty("role", out var role) && role.ValueKind == JsonValueKind.String)
                return role.GetString();
        }
        return null;
    }

    /// <summary>The engine's own tool that hands a question to a role (core's sub-sessions).</summary>
    private const string StartSubSession = "StartSubSession";
}
