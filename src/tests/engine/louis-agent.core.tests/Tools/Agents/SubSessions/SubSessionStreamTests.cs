using System.Text;
using LouisAgent.Core.Tools.Agents.SubSessions;

namespace LouisAgent.Core.Tests.Tools.Agents.SubSessions;

// S (CL15): a sub-session's SSE stream read to the end: the text deltas joined, the usage from message_delta (or from
// the error event, FX6), the error's message, and every event handed on as it arrives (the live view, F26). Phase 3
// step 1c replaces the loop with the one session-stream reader; these tests are its contract.
public class SubSessionStreamTests
{
    private const string Answer = """
        event: message_start
        data: {"type":"message_start","message":{"session_id":"sess_child"}}

        event: content_block_delta
        data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Fixed the null check; "}}

        event: content_block_delta
        data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"14/14 tests pass."}}

        event: message_delta
        data: {"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{"input_tokens":1000,"cache_creation_input_tokens":0,"cache_read_input_tokens":9000,"output_tokens":200,"requests":3,"cost":0.0123,"currency":"USD"}}

        event: message_stop
        data: {"type":"message_stop"}

        """;

    private const string Failed = """
        event: error
        data: {"type":"error","error":{"type":"api_error","message":"the model refused"},"usage":{"input_tokens":500,"cache_creation_input_tokens":0,"cache_read_input_tokens":0,"output_tokens":50,"requests":2,"cost":0.002,"currency":"USD"}}

        """;

    private static Stream Of(string sse) => new MemoryStream(Encoding.UTF8.GetBytes(sse));

    [Test]
    public async Task TheText_TheUsage_AndEveryEvent()
    {
        var events = new List<string>();

        var (text, usage, error) = await SubSessionStream.ReadAsync(Of(Answer), (type, _) => events.Add(type), CancellationToken.None);

        Assert.That(text, Is.EqualTo("Fixed the null check; 14/14 tests pass."));
        Assert.That(usage, Is.EqualTo(new SubSessionUsage(1000, 0, 9000, 200, 3, 0.0123m, "USD")));
        Assert.That(error, Is.Null);
        Assert.That(events, Is.EqualTo(new[] { "message_start", "content_block_delta", "content_block_delta", "message_delta", "message_stop" }));
    }

    [Test]
    public async Task AnErrorEvent_IsTheError_WithWhatWasCharged()
    {
        var (text, usage, error) = await SubSessionStream.ReadAsync(Of(Failed), (_, _) => { }, CancellationToken.None);

        Assert.That(text, Is.Empty);
        Assert.That(error, Is.EqualTo("the model refused"));
        Assert.That(usage, Is.EqualTo(new SubSessionUsage(500, 0, 0, 50, 2, 0.002m, "USD")));
    }

    [Test]
    public async Task NoEvents_IsNothing()
    {
        var (text, usage, error) = await SubSessionStream.ReadAsync(Of(""), (_, _) => { }, CancellationToken.None);

        Assert.That((text, usage, error), Is.EqualTo(("", (SubSessionUsage?)null, (string?)null)));
    }
}
