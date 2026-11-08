using LouisAgent.SessionStream;

namespace LouisAgent.Web.Chats.Replay;

/// <summary>
/// How long each event of a recorded session waits before it is drawn (F120). A recording is the stream the API sent,
/// which carries no times, so the wait comes from what the event draws: the answer types at reading speed, thinking
/// runs by faster, and a tool call or its result takes a beat, so a visitor sees the agent act.
/// </summary>
/// <remarks>
/// A switch on the event's shape, one arm per kind read like a table (ADR 0051), the numbers in one place.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0051-flat-conditionals-and-named-endpoints.md">ADR 0051: flat conditionals and named endpoints</seealso>
public static class RecordingPace
{
    private const int Word = 22;
    private const int Thought = 6;
    private const int Block = 150;
    private const int ToolCall = 450;
    private const int ToolResult = 700;

    /// <summary>The wait before an event, from its kind; a sub-session's event waits as its own kind would.</summary>
    public static int WaitMs(SessionEvent streamEvent) => streamEvent switch
    {
        ContentBlockDelta delta when Has(delta, "delta", "thinking") => Thought,
        ContentBlockDelta => Word,
        ContentBlockStarted start when Kind(start) == "tool_use" => ToolCall,
        ContentBlockStarted start when Kind(start) == "tool_result" => ToolResult,
        ContentBlockStarted => Block,
        SubSessionStreamed sub => WaitMs(sub.Inner),
        _ => 0,
    };

    /// <summary>The events in order, each with its wait.</summary>
    public static List<ReplayStep> Steps(IEnumerable<SessionEvent> events) =>
        events.Select(streamEvent => new ReplayStep(streamEvent, WaitMs(streamEvent))).ToList();

    /// <summary>Whether the event's object under this name has this property.</summary>
    private static bool Has(SessionEvent streamEvent, string name, string property) =>
        streamEvent.Data.TryGetProperty(name, out var inner) && inner.TryGetProperty(property, out _);

    /// <summary>The kind of block a start opens: thinking, text, tool_use or tool_result.</summary>
    private static string? Kind(ContentBlockStarted start) =>
        start.Data.TryGetProperty("content_block", out var block) && block.TryGetProperty("type", out var type)
            ? type.GetString()
            : null;
}
