using System.Text.Json;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Tests.Trials;

/// <summary>
/// Builds the trials the general checks are graded on: canned stream events, and turns with their durations and
/// failures.
/// </summary>
internal static class TrialBuilder
{
    /// <summary>The mode every built trial ran in.</summary>
    public static readonly EvalMode Mode = new(SampleMode.Beta, "puls/voice-af",
        [SampleToolset.Gamma, SampleToolset.Alpha]);

    /// <summary>A trial of the bobotie scenario made of these turns.</summary>
    public static Trial Trial(params TurnTranscript[] turns) => Build(Mode, default, turns);

    /// <summary>
    /// A trial of the bobotie scenario in this mode, its <c>expected</c> the given JSON, made of these turns.
    /// </summary>
    public static Trial Trial(EvalMode mode, string expected, params TurnTranscript[] turns) =>
        Build(mode, Json(expected), turns);

    /// <summary>The trial itself, from a mode and the scenario's success criteria.</summary>
    private static Trial Build(EvalMode mode, JsonElement expected, TurnTranscript[] turns) =>
        new("run_1", mode, 1,
            new EvalScenario("bobotie", new EvalInput(null, "Africa/Johannesburg", ["Hallo"]), expected, [],
                DatasetSplit.Dev),
            new Transcript(turns));

    /// <summary>A turn with these events, how long it took, and the infrastructure's failure if it had one.</summary>
    public static TurnTranscript Turn(int number, IReadOnlyList<SessionEvent>? events = null, double seconds = 1,
        TurnFailure? failure = null) =>
        new(number, "sess_1", "Hallo", events ?? [], TimeSpan.FromSeconds(seconds), failure);

    /// <summary>
    /// The usage a turn's message_delta carries; <paramref name="cost"/> is JSON text: a number, or null.
    /// </summary>
    public static SessionEvent Usage(string cost, int unpriced = 0, string? currency = null) =>
        SessionEvent.From("message_delta", Json(
            """{"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{"cost":COST,"unpriced":UNPRICED,"currency":CURRENCY}}"""
                .Replace("COST", cost).Replace("UNPRICED", unpriced.ToString())
                .Replace("CURRENCY", currency is null ? "null" : $"\"{currency}\"")));

    /// <summary>
    /// The usage a turn's message_delta carries with its four token counts, as the stream reports them.
    /// </summary>
    public static SessionEvent Tokens(int input, int cacheCreation, int cacheRead, int output) =>
        SessionEvent.From("message_delta", Json(
            """{"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{"input_tokens":IN,"cache_creation_input_tokens":CW,"cache_read_input_tokens":CR,"output_tokens":OUT,"cost":0.01,"unpriced":0}}"""
                .Replace("IN", input.ToString()).Replace("CW", cacheCreation.ToString())
                .Replace("CR", cacheRead.ToString()).Replace("OUT", output.ToString())));

    /// <summary>The agent's error, as the stream sends it.</summary>
    public static SessionEvent Error() =>
        SessionEvent.From("error", Json("""{"type":"error","error":{"type":"api_error","message":"overloaded"}}"""));

    /// <summary>An error whose usage is still reported, as the stream reports a failed turn's usage.</summary>
    public static SessionEvent ErrorWithUsage(string cost) =>
        SessionEvent.From("error",
            Json(
                """{"type":"error","error":{"type":"api_error","message":"overloaded"},"usage":{"cost":COST,"unpriced":0}}"""
                    .Replace("COST", cost)));

    /// <summary>The start of a StartSubSession call to a role, as the stream sends it.</summary>
    public static SessionEvent Call(string role) =>
        SessionEvent.From("content_block_start",
            Json(
                """{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t1","name":"StartSubSession","input":{"agent":"puls","role":"ROLE"}}}"""
                    .Replace("ROLE", role)));

    /// <summary>The start of the agent's own call to a tool, with its input as JSON, as the stream sends it.</summary>
    public static SessionEvent ToolCall(string tool, string input = "{}") =>
        SessionEvent.From("content_block_start",
            Json(
                """{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t1","name":"TOOL","input":INPUT}}"""
                    .Replace("TOOL", tool).Replace("INPUT", input)));

    /// <summary>
    /// A piece of the agent's answer, as the stream sends it; the text is written as a JSON string, so quotes and line
    /// breaks in it stay text.
    /// </summary>
    public static SessionEvent Text(string text) =>
        SessionEvent.From("content_block_delta",
            Json("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":TEXT}}"""
                .Replace("TEXT", JsonSerializer.Serialize(text))));

    /// <summary>
    /// A specialist's error, carried on the turn's stream as a sub_session event (it is not the turn's error).
    /// </summary>
    public static SessionEvent SubError() =>
        SessionEvent.From("sub_session",
            Json(
                """{"tool_use_id":"t0","agent":"puls","event":"error","data":{"type":"error","error":{"type":"api_error","message":"overloaded"}}}"""));

    /// <summary>
    /// A specialist's own StartSubSession call, carried as a sub_session event (it is not the turn's route).
    /// </summary>
    public static SessionEvent SubCall(string role) =>
        SessionEvent.From("sub_session",
            Json(
                """{"tool_use_id":"t0","agent":"puls","event":"content_block_start","data":{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t2","name":"StartSubSession","input":{"agent":"puls","role":"ROLE"}}}}"""
                    .Replace("ROLE", role)));

    /// <summary>A specialist's call to a tool, carried as a sub_session event.</summary>
    public static SessionEvent SubToolCall(string tool) =>
        SessionEvent.From("sub_session",
            Json(
                """{"tool_use_id":"t0","agent":"puls","event":"content_block_start","data":{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t2","name":"TOOL","input":{}}}}"""
                    .Replace("TOOL", tool)));

    /// <summary>A specialist's usage, carried as a sub_session event (the turn's usage is the top-level one).</summary>
    public static SessionEvent SubUsage(string cost) =>
        SessionEvent.From("sub_session",
            Json(
                """{"tool_use_id":"t0","agent":"puls","event":"message_delta","data":{"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{"cost":COST,"unpriced":0}}}"""
                    .Replace("COST", cost)));

    /// <summary>JSON text as an element that outlives its document.</summary>
    public static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();
}
