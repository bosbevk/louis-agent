using LouisAgent.Choices;
using LouisAgent.Evals.Puls.Evals.Cook;
using LouisAgent.Evals.Puls.Shared;
using System.Text.Json;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Puls.Tests;

/// <summary>Trials built from canned events, as the Puls evals' tests need them (F18 steps 5.3 and 5.4).</summary>
internal static class PulsTrials
{
    public const string Role = "puls/voice-af";
    public static readonly EvalMode CookEvalMode = new(CookMode.Cook.Wire(), Role, [PulsToolset.Specialists.Wire(), PulsToolset.Web.Wire()]);
    public static readonly EvalMode ToolsEvalMode = new(CookMode.Tools.Wire(), Role, [PulsToolset.Web.Wire(), PulsToolset.Shops.Wire()]);

    /// <summary>A trial of a scenario with the given <c>expected</c> JSON and place (none by default).</summary>
    public static Trial Trial(EvalMode mode, string task, string expected, string? place, params TurnTranscript[] turns) =>
        new("run_1", mode, 1, new EvalScenario(task, new EvalInput(place, "Africa/Johannesburg", ["x"]), JsonDocument.Parse(expected).RootElement.Clone(), [], DatasetSplit.Dev),
            new Transcript(turns));

    public static Trial Trial(EvalMode mode, string task, params TurnTranscript[] turns) => Trial(mode, task, "{}", null, turns);

    public static TurnTranscript Turn(int number, IReadOnlyList<SessionEvent> events, TrialFailure? failure = null) =>
        new(number, "sess_1", "q", events, TimeSpan.FromSeconds(1), failure);

    public static SessionEvent Start(string role) =>
        Event("content_block_start", """{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t1","name":"StartSubSession","input":{"role":"ROLE"}}}""".Replace("ROLE", role));

    public static SessionEvent Fetch(string url) =>
        Event("content_block_start", """{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t2","name":"FetchUrl","input":{"url":"URL"}}}""".Replace("URL", url));

    /// <summary>A tool call the cook made, as the stream carries it: inside a sub_session event, not the turn's own.</summary>
    public static SessionEvent SpecialistCall(string name, string inputJson) =>
        Event("sub_session", """{"tool_use_id":"t0","agent":"puls","event":"content_block_start","data":{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t3","name":"NAME","input":INPUT}}}"""
            .Replace("NAME", name).Replace("INPUT", inputJson));

    /// <summary>What a tool the cook called returned, as the stream carries it: a tool_result inside a sub_session event.</summary>
    public static SessionEvent SpecialistResult(string text) =>
        Event("sub_session", """{"tool_use_id":"t0","agent":"puls","event":"content_block_start","data":{"type":"content_block_start","index":0,"content_block":{"type":"tool_result","tool_use_id":"t3","content":CONTENT}}}"""
            .Replace("CONTENT", JsonSerializer.Serialize(text)));

    public static SessionEvent Text(string text) =>
        Event("content_block_delta", JsonSerializer.Serialize(new { type = "content_block_delta", index = 0, delta = new { type = "text_delta", text } }));

    public static SessionEvent Error() =>
        Event("error", """{"type":"error","error":{"type":"api_error","message":"overloaded"}}""");

    public static SessionEvent Event(string type, string json) => SessionEvent.From(type, JsonDocument.Parse(json).RootElement.Clone());
}
