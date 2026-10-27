using System.Text.Json;
using LouisAgent.SessionStream;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Chats;
using LouisAgent.Web.Shared.Usage;

namespace LouisAgent.Web.Shared.Tests.Chats;

// F31 step 7a: one recorded Puls turn (tests/louis-agent.session-stream.tests/Fixtures/session-stream.sse) is the contract between the session
// stream's readers. The Python harness (benchmarks/puls_check/stream.py) is held to the counts in
// session-stream.expected.json by its unittest; this holds the web app's TurnStream to the same ones, read the way the
// app reads them (SseParser, then TurnStream.Apply). A change to the stream re-records the fixture, and both fail
// until both readers follow (CL11: one C# reader under the three). T1 moved it here from the API's tests, where it
// never belonged, and added a case per event the stream folds.
public class TurnStreamTests
{
    private static string[] Strings(JsonElement e, string name) => e.GetProperty(name).EnumerateArray().Select(v => v.GetString()!).ToArray();

    [Test]
    public async Task TurnStream_ReadsPulssCallsAndTheSpecialistsAsTheHarnessDoes()
    {
        var turn = await SessionStreamFixture.ReplayAsync();
        var expected = SessionStreamFixture.Expected();

        var tools = turn.Blocks.Where(b => b.Kind == BlockKind.Tool).ToList();
        Assert.That(tools.Select(b => b.ToolName), Is.EqualTo(Strings(expected, "puls_tools")));
        var delegation = tools.Single(b => b.ToolName == "StartSubSession");
        Assert.That(delegation.ToolInput, Does.Contain(expected.GetProperty("role").GetString()));
        var specialist = delegation.SubTurn!.Blocks.Where(b => b.Kind == BlockKind.Tool).Select(b => b.ToolName);
        Assert.That(specialist, Is.EqualTo(Strings(expected, "specialist_tools")));
    }

    [Test]
    public async Task TurnStream_ReadsTheAnswerAsTheHarnessDoes()
    {
        var turn = await SessionStreamFixture.ReplayAsync();
        var expected = SessionStreamFixture.Expected();

        string answer = string.Concat(turn.Blocks.Where(b => b.Kind == BlockKind.Text).Select(b => b.Text)).Trim();
        Assert.That(answer, Does.StartWith(expected.GetProperty("answer_starts").GetString()));
        Assert.That(answer.Length, Is.EqualTo(expected.GetProperty("answer_length").GetInt32()));
        Assert.That(turn.Error, Is.Null);
        Assert.That(turn.StopReason, Is.Not.Null);
    }

    // ---- One case per event the stream folds. ----

    private static SessionEvent Event(string type, string json) => SessionEvent.From(type, JsonDocument.Parse(json).RootElement);

    private static Turn Apply(params (string Type, string Json)[] events)
    {
        var turn = new Turn();
        foreach (var (type, json) in events) TurnStream.Apply(turn, Event(type, json));
        return turn;
    }

    [Test]
    public void MessageStart_SetsTheToolsetsAndTheRole()
    {
        var turn = Apply(("message_start", """{"type":"message_start","message":{"session_id":"s","role":"assistant","toolsets":["web","shops"],"session_role":"puls-cook"}}"""));

        Assert.That(turn.Toolsets, Is.EqualTo(new[] { "web", "shops" }));
        Assert.That(turn.Role, Is.EqualTo("puls-cook"));
    }

    [Test]
    public void Text_OpensABlock_AndEveryDeltaAppendsToIt()
    {
        var turn = Apply(
            ("content_block_start", """{"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}"""),
            ("content_block_delta", """{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hallo"}}"""),
            ("content_block_delta", """{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":" daar"}}"""),
            ("content_block_stop", """{"type":"content_block_stop","index":0}"""));

        Assert.That(turn.Blocks, Has.Count.EqualTo(1));
        Assert.That(turn.Blocks[0].Kind, Is.EqualTo(BlockKind.Text));
        Assert.That(turn.Blocks[0].Text, Is.EqualTo("Hallo daar"));
    }

    [Test]
    public void Thinking_OpensABlock_AndItsStopRecordsHowLongItTook()
    {
        var turn = Apply(
            ("content_block_start", """{"type":"content_block_start","index":0,"content_block":{"type":"thinking","thinking":""}}"""),
            ("content_block_delta", """{"type":"content_block_delta","index":0,"delta":{"type":"thinking_delta","thinking":"Eers die winkel"}}"""));
        Assert.That(turn.Blocks[0].Kind, Is.EqualTo(BlockKind.Thinking));
        Assert.That(turn.Blocks[0].Text, Is.EqualTo("Eers die winkel"));
        Assert.That(turn.Blocks[0].ThinkingSeconds, Is.Null, "still thinking");

        TurnStream.Apply(turn, Event("content_block_stop", """{"type":"content_block_stop","index":0}"""));
        Assert.That(turn.Blocks[0].ThinkingSeconds, Is.Not.Null.And.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void AToolCall_AndItsResult_ShareOneBlock()
    {
        var turn = Apply(
            ("content_block_start", """{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"call_1","name":"WebSearch","input":{"query":"resep"}}}"""),
            ("content_block_stop", """{"type":"content_block_stop","index":0}"""),
            ("content_block_start", """{"type":"content_block_start","index":1,"content_block":{"type":"tool_result","tool_use_id":"call_1","content":"3 results","is_error":false}}"""),
            ("content_block_stop", """{"type":"content_block_stop","index":1}"""));

        Assert.That(turn.Blocks, Has.Count.EqualTo(1), "the result lands on the call's block");
        var call = turn.Blocks[0];
        Assert.That(call.Kind, Is.EqualTo(BlockKind.Tool));
        Assert.That(call.ToolName, Is.EqualTo("WebSearch"));
        Assert.That(call.ToolInput, Does.Contain("\"query\": \"resep\""), "the input, pretty-printed");
        Assert.That(call.ToolResult, Is.EqualTo("3 results"));
        Assert.That(call.ToolFailed, Is.False);
    }

    [Test]
    public void AFailedToolResult_MarksItsCall()
    {
        var turn = Apply(
            ("content_block_start", """{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"call_1","name":"FetchUrl","input":{}}}"""),
            ("content_block_start", """{"type":"content_block_start","index":1,"content_block":{"type":"tool_result","tool_use_id":"call_1","content":"Error: 404","is_error":true}}"""));

        Assert.That(turn.Blocks[0].ToolInput, Is.Null, "no arguments: nothing to show");
        Assert.That(turn.Blocks[0].ToolFailed, Is.True);
        Assert.That(turn.Blocks[0].ToolResult, Is.EqualTo("Error: 404"));
    }

    [Test]
    public void ASubSessionEvent_FoldsIntoTheDelegationCallsOwnTurn()
    {
        var turn = Apply(
            ("content_block_start", """{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"call_1","name":"StartSubSession","input":{"toolsets":["web"],"message":"Kook","role":"puls-cook"}}}"""),
            ("sub_session", """{"type":"sub_session","tool_use_id":"call_1","agent":"puls","event":"message_start","data":{"type":"message_start","message":{"session_id":"sub","role":"assistant","toolsets":["web","shops"],"session_role":"puls-cook"}}}"""),
            ("sub_session", """{"type":"sub_session","tool_use_id":"call_1","agent":"puls","event":"content_block_start","data":{"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}}"""),
            ("sub_session", """{"type":"sub_session","tool_use_id":"call_1","agent":"puls","event":"content_block_delta","data":{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Bobotie"}}}"""));

        var call = turn.Blocks.Single();
        Assert.That(call.SubAgent, Is.EqualTo("puls"));
        Assert.That(call.SubTurn, Is.Not.Null);
        Assert.That(call.SubTurn!.Toolsets, Is.EqualTo(new[] { "web", "shops" }));
        Assert.That(call.SubTurn.Role, Is.EqualTo("puls-cook"));
        Assert.That(call.SubTurn.Blocks.Single().Text, Is.EqualTo("Bobotie"));
    }

    [Test]
    public void MessageDelta_SetsTheStopReason_AndTheUsage()
    {
        var turn = Apply(("message_delta", """{"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{"input_tokens":4,"cache_creation_input_tokens":100,"cache_read_input_tokens":2000,"output_tokens":50,"requests":2,"cost":0.0031,"currency":"USD","unpriced":0}}"""));

        Assert.That(turn.StopReason, Is.EqualTo("end_turn"));
        Assert.That(turn.Usage, Is.EqualTo(new TurnUsage(4, 100, 2000, 50, 2, 0.0031m, "USD", 0)));
    }

    [Test]
    public void MessageDelta_WithoutUsage_LeavesItNull()
    {
        var turn = Apply(("message_delta", """{"type":"message_delta","delta":{"stop_reason":"end_turn"}}"""));

        Assert.That(turn.StopReason, Is.EqualTo("end_turn"));
        Assert.That(turn.Usage, Is.Null, "a command's turn, or an API without a ledger");
    }

    [Test]
    public void AnError_SetsTheMessage_AndKeepsWhatTheTurnUsed()
    {
        var turn = Apply(("error", """{"type":"error","error":{"type":"api_error","message":"TimeoutException: too slow"},"usage":{"input_tokens":4,"output_tokens":0,"requests":1,"cost":null,"currency":null,"unpriced":1}}"""));

        Assert.That(turn.Error, Is.EqualTo("TimeoutException: too slow"));
        Assert.That(turn.Usage, Is.EqualTo(new TurnUsage(4, 0, 0, 0, 1, null, null, 1)), "FX6: the failed turn's requests still count");
    }

    [Test]
    public void AnUnknownEvent_IsIgnored_NeverThrown()
    {
        var turn = new Turn();

        Assert.DoesNotThrow(() => TurnStream.Apply(turn, Event("ping", """{"type":"ping"}""")));
        Assert.That(turn.Blocks, Is.Empty);
        Assert.That(turn.Error, Is.Null);
    }
}
