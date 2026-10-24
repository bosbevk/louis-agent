using System.Text;
using System.Text.Json;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Puls.Turns;

/// <summary>One tool call a turn made: its name and its input, as the model sent it.</summary>
public sealed record ToolCall(string Name, JsonElement Input);

/// <summary>
/// What one turn says (the Python's <c>read_turn</c>): its answer, the calls Puls made, the calls its specialists made, the role
/// it handed to, the sub-session it continued, its error, and the results its tools returned. A specialist's error is the
/// specialist's, not the turn's; an empty role, handle or error is none, as the Python's truthiness reads it.
/// </summary>
public sealed record TurnView(string Answer, IReadOnlyList<ToolCall> Calls, IReadOnlyList<ToolCall> SpecialistCalls,
    string? Role, string? Continued, string? Error, IReadOnlyList<string> Results, IReadOnlyList<string> SpecialistResults)
{
    /// <summary>Reads a turn's events: the answer's text, its calls and results, and the role or sub-session it names.</summary>
    public static TurnView From(IEnumerable<SessionEvent> events)
    {
        var text = new StringBuilder();
        var calls = new List<ToolCall>();
        var specialistCalls = new List<ToolCall>();
        var results = new List<string>();
        var specialistResults = new List<string>();
        string? role = null, continued = null, error = null;

        void Read(SessionEvent streamEvent, bool specialist)
        {
            switch (streamEvent)
            {
                case ContentBlockStarted { Data: var data } when Object(data, "content_block") is { } block:
                    string type = Str(block, "type") ?? "";
                    if (type == "tool_result")
                        (specialist ? specialistResults : results).Add(ResultText(block));
                    else if (type == "tool_use")
                    {
                        string name = Str(block, "name") ?? "";
                        var input = Object(block, "input") ?? Empty;
                        (specialist ? specialistCalls : calls).Add(new ToolCall(name, input));
                        if (!specialist && name == "StartSubSession" && role is null) role = Truthy(Str(input, "role")) ?? Str(input, "personality");
                        if (!specialist && name == "ContinueSubSession" && continued is null) continued = Str(input, "handle");
                    }
                    break;
                case ContentBlockDelta { Data: var data } when Object(data, "delta") is { } delta && Str(delta, "type") == "text_delta":
                    if (!specialist && Str(delta, "text") is { } piece) text.Append(piece);
                    break;
                case SubSessionStreamed sub:
                    Read(sub.Inner, specialist: true);
                    break;
                case ErrorReported { Data: var data } when !specialist:
                    error = Object(data, "error") is { } detail && Str(detail, "message") is { Length: > 0 } message
                        ? message : data.GetRawText();
                    break;
            }
        }

        foreach (var streamEvent in events) Read(streamEvent, specialist: false);
        return new TurnView(text.ToString().Trim(), calls, specialistCalls, Truthy(role), Truthy(continued), Truthy(error),
            results, specialistResults);
    }

    private static readonly JsonElement Empty = JsonDocument.Parse("{}").RootElement.Clone();

    /// <summary>A tool result's text: its content when it's a string, else its JSON (a list of blocks, say), never a throw.</summary>
    private static string ResultText(JsonElement block) =>
        block.TryGetProperty("content", out var content)
            ? content.ValueKind == JsonValueKind.String ? content.GetString() ?? "" : content.GetRawText()
            : "";

    /// <summary>The object under a name, or null when it isn't one.</summary>
    private static JsonElement? Object(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Object
            ? value : null;

    /// <summary>The string under a name, or null when it's missing or isn't a string.</summary>
    private static string? Str(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() : null;

    private static string? Truthy(string? value) => string.IsNullOrEmpty(value) ? null : value;
}
