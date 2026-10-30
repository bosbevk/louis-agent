using System.Text.Json;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;

namespace LouisAgent.Identities.Puls.Evals.Checks.Shared.Turns;

/// <summary>One tool call a turn made: its name and its input, as the model sent it.</summary>
public sealed record ToolCall(string Name, JsonElement Input);

/// <summary>
/// What one turn says, as the Python's <c>read_turn</c> read it: its answer, the calls Puls made, the calls its
/// specialists made, the role it handed to, the sub-session it continued, its error, and the results its tools
/// returned.
/// </summary>
/// <remarks>
/// The answer, the role and the error come from <see cref="EngineEvents"/>, the one reader of them, so a check and the
/// protocol's facts can't disagree; the calls, the results and the handle are read here, since only an identity cares
/// about them. A specialist's error is the specialist's, not the turn's; an empty handle counts as none, as the
/// Python's truthiness read it.
/// </remarks>
public sealed record TurnView(
    string Answer,
    IReadOnlyList<ToolCall> Calls,
    IReadOnlyList<ToolCall> SpecialistCalls,
    string? Role,
    string? Continued,
    string? Error,
    IReadOnlyList<string> Results,
    IReadOnlyList<string> SpecialistResults)
{
    /// <summary>Reads some events as a turn: for the scorer, which holds events and no turn.</summary>
    public static TurnView From(IEnumerable<SessionEvent> events) =>
        From(new TurnTranscript(1, string.Empty, string.Empty, events.ToList(), TimeSpan.Zero, null));

    /// <summary>
    /// Reads a turn: the answer, the role and the error from <see cref="EngineEvents"/>; its calls, results and
    /// sub-session handle here.
    /// </summary>
    public static TurnView From(TurnTranscript turn)
    {
        var calls = new List<ToolCall>();
        var specialistCalls = new List<ToolCall>();
        var results = new List<string>();
        var specialistResults = new List<string>();
        string? continued = null;

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
                        if (!specialist && name == "ContinueSubSession" && continued is null)
                            continued = Str(input, "handle");
                    }

                    break;
                case SubSessionStreamed sub:
                    Read(sub.Inner, specialist: true);
                    break;
            }
        }

        foreach (var streamEvent in turn.Events) Read(streamEvent, specialist: false);
        return new TurnView(EngineEvents.AnswerOf(turn), calls, specialistCalls, EngineEvents.RoutedRole(turn),
            Truthy(continued),
            Truthy(EngineEvents.ErrorTextOf(turn)), results, specialistResults);
    }

    /// <summary>An empty input, for a tool call that sent none.</summary>
    private static readonly JsonElement Empty = JsonDocument.Parse("{}").RootElement.Clone();

    /// <summary>
    /// A tool result's text: its content when it's a string, else its JSON (a list of blocks, say), never a throw.
    /// </summary>
    private static string ResultText(JsonElement block) =>
        block.TryGetProperty("content", out var content)
            ? content.ValueKind == JsonValueKind.String ? content.GetString() ?? "" : content.GetRawText()
            : "";

    /// <summary>The object under a name, or null when it isn't one.</summary>
    private static JsonElement? Object(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.Object
            ? value
            : null;

    /// <summary>The string under a name, or null when it's missing or isn't a string.</summary>
    private static string? Str(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>An empty text counts as none, as the Python read it.</summary>
    private static string? Truthy(string? value) => string.IsNullOrEmpty(value) ? null : value;
}
