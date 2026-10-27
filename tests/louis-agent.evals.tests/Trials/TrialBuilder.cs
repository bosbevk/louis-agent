using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Tests.Trials;

/// <summary>Builds the trials the common graders are scored on: canned events, and turns with their durations and failures.</summary>
internal static class TrialBuilder
{
    public static readonly EvalMode Mode = new("puls-cook", "puls/voice-af", [SampleToolset.Gamma, SampleToolset.Alpha]);

    public static Trial Trial(params TurnTranscript[] turns) =>
        new("run_1", Mode, 1, new EvalScenario("bobotie", new EvalInput(null, "Africa/Johannesburg", ["Hallo"]), default, [], DatasetSplit.Dev),
            new Transcript(turns));

    public static TurnTranscript Turn(int number, IReadOnlyList<SessionEvent>? events = null, double seconds = 1, TrialFailure? failure = null) =>
        new(number, "sess_1", "Hallo", events ?? [], TimeSpan.FromSeconds(seconds), failure);

    /// <summary>The usage a turn's message_delta carries; <paramref name="cost"/> is JSON text: a number, or null.</summary>
    public static SessionEvent Usage(string cost, int unpriced = 0, string? currency = null) =>
        SessionEvent.From("message_delta", Json("""{"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{"cost":COST,"unpriced":UNPRICED,"currency":CURRENCY}}"""
            .Replace("COST", cost).Replace("UNPRICED", unpriced.ToString()).Replace("CURRENCY", currency is null ? "null" : $"\"{currency}\"")));

    public static SessionEvent Error() =>
        SessionEvent.From("error", Json("""{"type":"error","error":{"type":"api_error","message":"overloaded"}}"""));

    /// <summary>An error whose usage is still reported, as the stream reports a failed turn's usage.</summary>
    public static SessionEvent ErrorWithUsage(string cost) =>
        SessionEvent.From("error", Json("""{"type":"error","error":{"type":"api_error","message":"overloaded"},"usage":{"cost":COST,"unpriced":0}}""".Replace("COST", cost)));

    /// <summary>The start of a StartSubSession call to a role, as the stream sends it.</summary>
    public static SessionEvent Call(string role) =>
        SessionEvent.From("content_block_start", Json("""{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t1","name":"StartSubSession","input":{"agent":"puls","role":"ROLE"}}}""".Replace("ROLE", role)));

    public static SessionEvent Text(string text) =>
        SessionEvent.From("content_block_delta", Json("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"TEXT"}}""".Replace("TEXT", text)));

    /// <summary>A specialist's error, carried on the turn's stream as a sub_session event (it is not the turn's error).</summary>
    public static SessionEvent SubError() =>
        SessionEvent.From("sub_session", Json("""{"tool_use_id":"t0","agent":"puls","event":"error","data":{"type":"error","error":{"type":"api_error","message":"overloaded"}}}"""));

    /// <summary>A specialist's own StartSubSession call, carried as a sub_session event (it is not the turn's route).</summary>
    public static SessionEvent SubCall(string role) =>
        SessionEvent.From("sub_session", Json("""{"tool_use_id":"t0","agent":"puls","event":"content_block_start","data":{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t2","name":"StartSubSession","input":{"agent":"puls","role":"ROLE"}}}}""".Replace("ROLE", role)));

    /// <summary>A specialist's usage, carried as a sub_session event (the turn's usage is the top-level one).</summary>
    public static SessionEvent SubUsage(string cost) =>
        SessionEvent.From("sub_session", Json("""{"tool_use_id":"t0","agent":"puls","event":"message_delta","data":{"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{"cost":COST,"unpriced":0}}}""".Replace("COST", cost)));

    public static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();
}
