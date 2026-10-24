using System.Text.Json;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Trials;

/// <summary>
/// The one reader of the engine's events for the common graders (F18 step 4.2): whether a turn streamed an error, what it cost and in
/// which currency, which role its first sub-session went to, and the error's message. It reads the stream as the API sends it, and never
/// throws on an odd event: a field it can't read is read as absent, so the grade is Unknown or there is no route, not a crash. The cost is
/// the usage the stream reports on message_delta (or on error), the ledger's total for the turn; no price is worked out here.
/// </summary>
public static class EngineEvents
{
    /// <summary>The engine's own tool that hands a question to a role (core's sub-sessions).</summary>
    private const string StartSubSession = "StartSubSession";

    /// <summary>True when the turn streamed an error event: the agent's error. A specialist's error is not the turn's.</summary>
    public static bool StreamedError(TurnTranscript turn) => turn.Events.Any(e => e is ErrorReported);

    /// <summary>The infrastructure's failure of the turn (a timeout, a dropped connection), reported apart from the agent's errors.</summary>
    public static TrialFailure? InfrastructureFailure(TurnTranscript turn) => turn.Failure;

    /// <summary>True when the turn reported usage at all.</summary>
    public static bool HasUsage(TurnTranscript turn) => Usage(turn) is not null;

    /// <summary>The currency the turn's usage is in, or null when it doesn't say.</summary>
    public static string? CurrencyOf(TurnTranscript turn) => Usage(turn) is { } usage ? Text(usage, "currency") : null;

    /// <summary>The turn's cost in the ledger's currency; null when unknown (no usage, no cost, or a request with no price).</summary>
    public static decimal? CostOf(TurnTranscript turn)
    {
        if (Usage(turn) is not { } usage) return null;
        // An absent count is no unpriced request; any other count that isn't zero, or isn't a number, leaves the total incomplete.
        if (Field(usage, "unpriced", out var unpriced)
            && !(unpriced.ValueKind == JsonValueKind.Number && unpriced.TryGetInt32(out int count) && count == 0))
            return null;
        return Field(usage, "cost", out var cost) && cost.ValueKind == JsonValueKind.Number && cost.TryGetDecimal(out var value) ? value : null;
    }

    /// <summary>The role of the turn's first StartSubSession call that names a role, or null when the agent answered itself or no such call names one.</summary>
    public static string? RoutedRole(TurnTranscript turn)
    {
        foreach (var e in turn.Events)
        {
            if (e is not ContentBlockStarted || !Field(e.Data, "content_block", out var block)) continue;
            if (Text(block, "type") == "tool_use" && Text(block, "name") == StartSubSession
                && Field(block, "input", out var input) && Text(input, "role") is { } role && !string.IsNullOrWhiteSpace(role))
                return role;
        }
        return null;
    }

    /// <summary>The message of the turn's error event, or null when it has none.</summary>
    public static string? ErrorMessageOf(TurnTranscript turn)
    {
        var error = turn.Events.OfType<ErrorReported>().FirstOrDefault();
        return error is not null && Field(error.Data, "error", out var detail) ? Text(detail, "message") : null;
    }

    /// <summary>The usage object on the turn's last message_delta, or on its error when it failed: the stream's end. Null when there is none.</summary>
    private static JsonElement? Usage(TurnTranscript turn)
    {
        var end = turn.Events.LastOrDefault(e => e is MessageDelta or ErrorReported);
        return end is not null && Field(end.Data, "usage", out var usage) && usage.ValueKind == JsonValueKind.Object ? usage : null;
    }

    /// <summary>A field of a JSON object; false when the element is not an object, or has no such field.</summary>
    private static bool Field(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object) return element.TryGetProperty(name, out value);
        value = default;
        return false;
    }

    /// <summary>A string field of a JSON object; null when it is absent or isn't a string.</summary>
    private static string? Text(JsonElement element, string name) =>
        Field(element, name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
