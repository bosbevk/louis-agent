using System.Text.Json;
using LouisAgent.Evals.Contracts.Transcripts;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Transcripts;

/// <summary>
/// Writes down what happened in a turn from its stream: the answer as the agent sent it, each tool call with its
/// result as the agent got it, the turn's error, and what it cost. That is all the facts and the judge read.
/// </summary>
/// <remarks>
/// It reads only this turn's own events. An asked agent's events come nested under the call that asked it
/// (<c>sub_session</c>), and they are that agent's work: the call to it is already among this turn's calls, with
/// its answer as the result, and that is what the facts about another agent read. The events are read as the web's
/// chat reads them (<c>TurnStream</c>), from the shared reader's records.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0134-an-eval-is-one-file-of-conversations.md">ADR 0134: an eval is one file of conversations</seealso>
public static class TurnTranscripts
{
    private const string ContentBlock = "content_block";
    private const string ToolUse = "tool_use";
    private const string ToolResult = "tool_result";
    private const string TextDelta = "text_delta";
    private const string Usage = "usage";

    /// <summary>The turn written down: what was asked, its stream's events, and how long it took.</summary>
    public static TurnTranscript Of(string ask, IReadOnlyList<SessionEvent> events, TimeSpan took)
    {
        var blocks = events.OfType<ContentBlockStarted>().Select(start => start.Data.GetProperty(ContentBlock))
            .ToList();
        var results = blocks.Where(block => TypeOf(block) == ToolResult)
            .ToLookup(block => block.GetProperty("tool_use_id").GetString() ?? "");
        var calls = blocks.Where(block => TypeOf(block) == ToolUse)
            .Select(call => CallOf(call, results[call.GetProperty("id").GetString() ?? ""].LastOrDefault()))
            .ToList();

        string answer = string.Concat(events.OfType<ContentBlockDelta>().Select(TextOf));
        string? error = events.OfType<ErrorReported>().Select(MessageOf).LastOrDefault();
        return new TurnTranscript(ask, answer, calls, took, error);
    }

    /// <summary>
    /// What the turn cost, from the last usage it reported: on its end, or on its error when it failed. Nobody
    /// could price it, or it reported none: a null cost, never 0.
    /// </summary>
    public static TurnCost CostOf(IReadOnlyList<SessionEvent> events)
    {
        var usage = events.Where(e => e is MessageDelta or ErrorReported)
            .Select(e => e.Data.TryGetProperty(Usage, out var used) ? used : default)
            .LastOrDefault(used => used.ValueKind == JsonValueKind.Object);
        if (usage.ValueKind != JsonValueKind.Object) return TurnCost.None;

        decimal? amount = usage.TryGetProperty("cost", out var cost) && cost.ValueKind == JsonValueKind.Number
            ? cost.GetDecimal()
            : null;
        string? currency = usage.TryGetProperty("currency", out var code) && code.ValueKind == JsonValueKind.String
            ? code.GetString()
            : null;
        return new TurnCost(amount, currency);
    }

    /// <summary>One tool call with its result, empty when no result came back.</summary>
    private static ToolCallTranscript CallOf(JsonElement call, JsonElement result)
    {
        string tool = call.GetProperty("name").GetString() ?? "";
        string input = call.GetProperty("input").GetRawText();
        if (result.ValueKind != JsonValueKind.Object) return new ToolCallTranscript(tool, input, "");

        return new ToolCallTranscript(tool, input, result.GetProperty("content").GetString() ?? "",
            result.GetProperty("is_error").GetBoolean());
    }

    /// <summary>The piece of the answer a delta carries; thinking is never part of the answer.</summary>
    private static string TextOf(ContentBlockDelta delta)
    {
        var change = delta.Data.GetProperty("delta");
        return TypeOf(change) == TextDelta ? change.GetProperty("text").GetString() ?? "" : "";
    }

    /// <summary>The failure's own message.</summary>
    internal static string? MessageOf(ErrorReported error) =>
        error.Data.GetProperty("error").GetProperty("message").GetString();

    /// <summary>What kind of block or delta this is.</summary>
    private static string? TypeOf(JsonElement element) =>
        element.TryGetProperty("type", out var type) ? type.GetString() : null;
}
