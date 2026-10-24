using System.Text;
using System.Text.Json;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Puls.Transcripts;

/// <summary>One tool call a turn made: its name and its input, as the model sent it.</summary>
public sealed record ToolCall(string Name, JsonElement Input);

/// <summary>
/// What one turn says (the Python's <c>read_turn</c>): its answer, the calls Puls made, the calls its specialists made, the role
/// it handed to, the sub-session it continued, its error, and the results its tools returned.
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
                case ContentBlockStarted { Data: var data } when data.TryGetProperty("content_block", out var block)
                                                              && block.ValueKind == JsonValueKind.Object:
                    string type = block.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
                    if (type == "tool_result")
                        (specialist ? specialistResults : results).Add(block.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "");
                    else if (type == "tool_use")
                    {
                        string name = block.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                        var input = block.TryGetProperty("input", out var i) && i.ValueKind == JsonValueKind.Object ? i.Clone() : Empty;
                        (specialist ? specialistCalls : calls).Add(new ToolCall(name, input));
                        if (!specialist && name == "StartSubSession" && role is null) role = Text(input, "role") ?? Text(input, "personality");
                        if (!specialist && name == "ContinueSubSession" && continued is null) continued = Text(input, "handle");
                    }
                    break;
                case ContentBlockDelta { Data: var data } when data.TryGetProperty("delta", out var delta)
                                                              && delta.ValueKind == JsonValueKind.Object
                                                              && delta.TryGetProperty("type", out var kind)
                                                              && kind.GetString() == "text_delta":
                    if (!specialist && delta.TryGetProperty("text", out var piece)) text.Append(piece.GetString());
                    break;
                case SubSessionStreamed sub:
                    Read(sub.Inner, true);
                    break;
                case ErrorReported { Data: var data }:
                    error = data.TryGetProperty("error", out var e) && e.TryGetProperty("message", out var message)
                        ? message.GetString() : data.GetRawText();
                    break;
            }
        }

        foreach (var streamEvent in events) Read(streamEvent, specialist: false);
        return new TurnView(text.ToString().Trim(), calls, specialistCalls, role, continued, error, results, specialistResults);
    }

    private static readonly JsonElement Empty = JsonDocument.Parse("{}").RootElement.Clone();

    private static string? Text(JsonElement input, string name) =>
        input.ValueKind == JsonValueKind.Object && input.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() : null;
}
