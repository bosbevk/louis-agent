using System.Text.Json;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Puls.Tests;

/// <summary>Trials built from canned events, as the Puls evals' tests need them (F18 step 5.3).</summary>
internal static class PulsTrials
{
    public const string Role = "puls/voice-af";
    public static readonly EvalMode CookMode = new("puls-cook", Role, ["specialists", "web"]);
    public static readonly EvalMode ToolsMode = new("puls-tools", Role, ["web", "shops"]);

    public static Trial Trial(EvalMode mode, string task, params TurnTranscript[] turns) =>
        new("run_1", mode, 1, new EvalScenario(task, new EvalInput(null, "Africa/Johannesburg", ["x"]), default, [], DatasetSplit.Dev), new Transcript(turns));

    public static TurnTranscript Turn(int number, IReadOnlyList<SessionEvent> events, TrialFailure? failure = null) =>
        new(number, "sess_1", "q", events, TimeSpan.FromSeconds(1), failure);

    public static SessionEvent Start(string role) =>
        Event("content_block_start", """{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t1","name":"StartSubSession","input":{"role":"ROLE"}}}""".Replace("ROLE", role));

    public static SessionEvent Fetch(string url) =>
        Event("content_block_start", """{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t2","name":"FetchUrl","input":{"url":"URL"}}}""".Replace("URL", url));

    public static SessionEvent Text(string text) =>
        Event("content_block_delta", """{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"TEXT"}}""".Replace("TEXT", text));

    public static SessionEvent Error() =>
        Event("error", """{"type":"error","error":{"type":"api_error","message":"overloaded"}}""");

    public static SessionEvent Event(string type, string json) => SessionEvent.From(type, JsonDocument.Parse(json).RootElement.Clone());
}
