using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Evals;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using LouisAgent.Identities.Puls.Evals.Checks.Cook;
using LouisAgent.Identities.Puls.Evals.Checks.Shared;
using LouisAgent.SessionStream;

namespace LouisAgent.Identities.Puls.Tests.Evals.Checks;

/// <summary>Trials built from canned stream events, the way the Puls checks' tests need them.</summary>
internal static class PulsTrials
{
    /// <summary>The role Puls's modes open with: its Afrikaans voice.</summary>
    public const string Role = "puls/voice-af";

    /// <summary>The cook's mode: Puls hands a meal to its cook.</summary>
    public static readonly EvalMode CookEvalMode =
        new(CookMode.Cook.Wire(), Role, [PulsToolset.Specialists.Wire(), PulsToolset.Web.Wire()]);

    /// <summary>The tools mode: Puls answers a meal itself, with the shop tools.</summary>
    public static readonly EvalMode ToolsEvalMode =
        new(CookMode.Tools.Wire(), Role, [PulsToolset.Web.Wire(), PulsToolset.Shops.Wire()]);

    /// <summary>A trial of a scenario with the given <c>expected</c> JSON and place (none by default).</summary>
    public static Trial Trial(EvalMode mode, string task, string expected, string? place,
        params TurnTranscript[] turns) =>
        new("run_1", mode, 1,
            new EvalScenario(task, new EvalInput(place, "Africa/Johannesburg", ["x"]),
                JsonDocument.Parse(expected).RootElement.Clone(), [], DatasetSplit.Dev),
            new Transcript(turns));

    /// <summary>A trial of a scenario that expects nothing and names no place.</summary>
    public static Trial Trial(EvalMode mode, string task, params TurnTranscript[] turns) =>
        Trial(mode, task, "{}", null, turns);

    /// <summary>One turn of the conversation, made of these events, failed or not.</summary>
    public static TurnTranscript Turn(int number, IReadOnlyList<SessionEvent> events, TurnFailure? failure = null) =>
        new(number, "sess_1", "q", events, TimeSpan.FromSeconds(1), failure);

    /// <summary>Puls starting a sub-session with this role, as the stream carries it.</summary>
    public static SessionEvent Start(string role) =>
        Event("content_block_start",
            """{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t1","name":"StartSubSession","input":{"role":"ROLE"}}}"""
                .Replace("ROLE", role));

    /// <summary>Puls fetching a page itself, as the stream carries it.</summary>
    public static SessionEvent Fetch(string url) =>
        Event("content_block_start",
            """{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t2","name":"FetchUrl","input":{"url":"URL"}}}"""
                .Replace("URL", url));

    /// <summary>
    /// A tool call the cook made, as the stream carries it: inside a sub_session event, not the turn's own.
    /// </summary>
    public static SessionEvent SpecialistCall(string name, string inputJson) =>
        Event("sub_session",
            """{"tool_use_id":"t0","agent":"puls","event":"content_block_start","data":{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t3","name":"NAME","input":INPUT}}}"""
                .Replace("NAME", name).Replace("INPUT", inputJson));

    /// <summary>
    /// What a tool the cook called returned, as the stream carries it: a tool_result inside a sub_session event.
    /// </summary>
    public static SessionEvent SpecialistResult(string text) =>
        Event("sub_session",
            """{"tool_use_id":"t0","agent":"puls","event":"content_block_start","data":{"type":"content_block_start","index":0,"content_block":{"type":"tool_result","tool_use_id":"t3","content":CONTENT}}}"""
                .Replace("CONTENT", JsonSerializer.Serialize(text)));

    /// <summary>A piece of the answer's text, as the stream carries it.</summary>
    public static SessionEvent Text(string text) =>
        Event("content_block_delta",
            JsonSerializer.Serialize(new
            {
                type = "content_block_delta",
                index = 0,
                delta = new
                {
                    type = "text_delta",
                    text
                }
            }));

    /// <summary>The provider's error in the middle of a turn: it was overloaded.</summary>
    public static SessionEvent Error() =>
        Event("error", """{"type":"error","error":{"type":"api_error","message":"overloaded"}}""");

    /// <summary>Any stream event, from its type and its JSON.</summary>
    public static SessionEvent Event(string type, string json) =>
        SessionEvent.From(type, JsonDocument.Parse(json).RootElement.Clone());
}
