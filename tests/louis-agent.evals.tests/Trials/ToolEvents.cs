using System.Text.Json;
using LouisAgent.SessionStream;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;

namespace LouisAgent.Evals.Tests.Trials;

/// <summary>
/// Tool calls and their results with ids of their own, as the stream sends them, so two calls to one tool each find
/// the result that is theirs. A specialist's are carried one level down, as sub_session events.
/// </summary>
internal static class ToolEvents
{
    /// <summary>The agent's own call to a tool, under this id.</summary>
    public static SessionEvent Use(string id, string tool) =>
        SessionEvent.From("content_block_start", Block(Called(id, tool)));

    /// <summary>What a tool gave the agent back for the call with this id.</summary>
    public static SessionEvent Got(string id, string text) =>
        SessionEvent.From("content_block_start", Block(Gave(id, text)));

    /// <summary>A specialist's call to a tool, under this id, carried as a sub_session event.</summary>
    public static SessionEvent SubUse(string id, string tool) => Carried(Called(id, tool));

    /// <summary>What a tool gave a specialist back for the call with this id, carried as a sub_session event.</summary>
    public static SessionEvent SubGot(string id, string text) => Carried(Gave(id, text));

    /// <summary>The content block of a call.</summary>
    private static string Called(string id, string tool) =>
        $$$"""{"type":"tool_use","id":"{{{id}}}","name":"{{{tool}}}","input":{}}""";

    /// <summary>
    /// The content block of a result; the text is written as a JSON string, so quotes and line breaks stay text.
    /// </summary>
    private static string Gave(string id, string text) =>
        $$$"""{"type":"tool_result","tool_use_id":"{{{id}}}","content":{{{JsonSerializer.Serialize(text)}}}}""";

    /// <summary>A content block as the start event of the stream.</summary>
    private static JsonElement Block(string block) =>
        Json($$"""{"type":"content_block_start","index":0,"content_block":{{block}}}""");

    /// <summary>A content block as a specialist's event, carried on the turn's stream.</summary>
    private static SessionEvent Carried(string block) =>
        SessionEvent.From("sub_session", Json(
            $$"""
              {"tool_use_id":"t0","agent":"puls","event":"content_block_start","data":{{Block(block).GetRawText()}}}
              """));
}
