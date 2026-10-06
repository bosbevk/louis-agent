namespace louis_agent.orchestrator.tests;

public class LouisAgentClientTests
{
    // The shape louis-agent.api streams (see ClaudeStyleStream in src/louis-agent.api/Program.cs).
    private const string Stream = """
        event: message_start
        data: {"type":"message_start","message":{"session_id":"sess_1","role":"assistant"}}

        event: content_block_start
        data: {"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t1","name":"DotNetTest","input":{}}}

        event: content_block_stop
        data: {"type":"content_block_stop","index":0}

        event: content_block_start
        data: {"type":"content_block_start","index":1,"content_block":{"type":"tool_result","tool_use_id":"t1","content":"Error: 1 failed","is_error":true}}

        event: content_block_start
        data: {"type":"content_block_start","index":2,"content_block":{"type":"text","text":""}}

        event: content_block_delta
        data: {"type":"content_block_delta","index":2,"delta":{"type":"text_delta","text":"Fixed it.\nFIX-RESULT: "}}

        event: content_block_delta
        data: {"type":"content_block_delta","index":2,"delta":{"type":"text_delta","text":"{\"status\":\"fixed\",\"commit\":\"abc1234\",\"branch\":\"fix/x\"}"}}

        event: message_delta
        data: {"type":"message_delta","delta":{"stop_reason":"end_turn"}}

        event: message_stop
        data: {"type":"message_stop"}

        """;

    [Test]
    public async Task ReadStream_CollectsTextToolCallsAndStopReason()
    {
        var progress = new List<string>();
        var reply = await LouisAgentClient.ReadStreamAsync(new StringReader(Stream), progress.Add);

        Assert.That(reply.ToolCalls, Is.EqualTo(new[] { "DotNetTest" }));
        Assert.That(reply.StopReason, Is.EqualTo("end_turn"));
        Assert.That(reply.Error, Is.Null);
        Assert.That(reply.Text, Does.StartWith("Fixed it."));
        Assert.That(progress, Has.Some.Contains("DotNetTest"));
        Assert.That(progress, Has.Some.Contains("error"));
    }

    [Test]
    public async Task ReadStream_ReportsAnErrorEvent()
    {
        const string stream = """
            event: error
            data: {"type":"error","error":{"type":"api_error","message":"prompt is too long"}}

            """;
        var reply = await LouisAgentClient.ReadStreamAsync(new StringReader(stream));

        Assert.That(reply.Error, Is.EqualTo("prompt is too long"));
    }

    [Test]
    public async Task ParseFixResult_ReadsTheStreamedResultLine()
    {
        var reply = await LouisAgentClient.ReadStreamAsync(new StringReader(Stream));
        var result = LouisAgentClient.ParseFixResult(reply.Text);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Status, Is.EqualTo("fixed"));
        Assert.That(result.Commit, Is.EqualTo("abc1234"));
        Assert.That(result.Branch, Is.EqualTo("fix/x"));
    }

    [Test]
    public void ParseFixResult_UsesTheLastLine_AndAcceptsBackticks()
    {
        const string text = """
            I will end with FIX-RESULT: {"status":"draft"}
            Done.
            `FIX-RESULT: {"status":"failed","reason":"tests keep failing"}`
            """;
        var result = LouisAgentClient.ParseFixResult(text);

        Assert.That(result!.Status, Is.EqualTo("failed"));
        Assert.That(result.Reason, Is.EqualTo("tests keep failing"));
    }

    [TestCase("All done, no result line.")]
    [TestCase("FIX-RESULT: {not json}")]
    [TestCase("FIX-RESULT: {\"commit\":\"abc1234\"}")]
    public void ParseFixResult_WithoutAValidLine_IsNull(string text) =>
        Assert.That(LouisAgentClient.ParseFixResult(text), Is.Null);
}
