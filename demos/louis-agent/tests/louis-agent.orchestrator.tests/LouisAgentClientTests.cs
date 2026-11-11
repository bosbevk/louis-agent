using LouisAgent.Composition.Tools;
using LouisAgent.Core.Usage.Reports;
using LouisAgent.Orchestrator.Client;

namespace LouisAgent.Orchestrator.Tests;

/// <summary>
/// How the orchestrator reads louis-agent's answer: the stream folded into text, tool calls, a stop reason and what it
/// used, and the FIX-RESULT line a fix ends with, even when the model wrote it as broken JSON.
/// </summary>
public class LouisAgentClientTests
{
    /// <summary>The stream as the client reads it: the same SSE text, as bytes.</summary>
    private static MemoryStream Bytes(string sse) => new(System.Text.Encoding.UTF8.GetBytes(sse));

    /// <summary>
    /// A fix as louis-agent.api streams it (the Claude format in src/hosts/louis-agent.api/Sessions/Streaming/Claude):
    /// a failed tool call, then the answer with its FIX-RESULT line.
    /// </summary>
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

    /// <summary>
    /// The answer's text, each tool call with its result, and how the turn ended, with progress as it goes.
    /// </summary>
    [Test]
    public async Task ReadStream_CollectsTextToolCallsAndStopReason()
    {
        var progress = new List<string>();
        var reply = await LouisAgentClient.ReadStreamAsync(Bytes(Stream), progress.Add);

        Assert.That(reply.ToolCalls.Select(t => t.Name), Is.EqualTo(new[] { "DotNetTest" }));
        Assert.That(reply.ToolCalls[0].Result, Is.EqualTo("Error: 1 failed"));
        Assert.That(reply.ToolCalls[0].IsError, Is.True);
        Assert.That(reply.StopReason, Is.EqualTo("end_turn"));
        Assert.That(reply.Error, Is.Null);
        Assert.That(reply.Text, Does.StartWith("Fixed it."));
        Assert.That(progress, Has.Some.Contains("DotNetTest"));
        Assert.That(progress, Has.Some.Contains("error"));
    }

    /// <summary>The turn's usage comes from message_delta, with Claude's field names (F3).</summary>
    [Test]
    public async Task ReadStream_ReadsTheTurnsUsageFromMessageDelta()
    {
        const string stream = """
                              event: message_delta
                              data: {"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{"input_tokens":1900,"cache_creation_input_tokens":3000,"cache_read_input_tokens":26000,"output_tokens":412,"requests":3,"cost":0.0061,"currency":"USD","unpriced":0}}

                              """;

        var reply = await LouisAgentClient.ReadStreamAsync(Bytes(stream));

        Assert.That(reply.Usage, Is.EqualTo(new UsageTotal("louis-agent", 3, 1_900, 3_000, 26_000, 412, 0.0061m, 0)));
        Assert.That(reply.Currency, Is.EqualTo("USD"));
    }

    /// <summary>Usage the API couldn't price has no cost, never a cost of 0.</summary>
    [Test]
    public async Task ReadStream_UnpricedUsage_HasNoCost()
    {
        const string stream = """
                              event: message_delta
                              data: {"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{"input_tokens":2050,"cache_creation_input_tokens":0,"cache_read_input_tokens":0,"output_tokens":2,"requests":1,"cost":null,"currency":null,"unpriced":1}}

                              """;

        var reply = await LouisAgentClient.ReadStreamAsync(Bytes(stream));

        Assert.That((reply.Usage!.Cost, reply.Usage.Unpriced, reply.Currency),
            Is.EqualTo(((decimal?)null, 1, (string?)null)));
    }

    /// <summary>
    /// A failed reply keeps its error and what it used: the requests made before the failure were charged, and the
    /// error event carries them (FX6).
    /// </summary>
    [Test]
    public async Task ReadStream_FailedReply_KeepsTheErrorAndItsUsage()
    {
        const string stream = """
                              event: error
                              data: {"type":"error","error":{"type":"api_error","message":"HttpRequestException: 529 overloaded"},"usage":{"input_tokens":1200,"cache_creation_input_tokens":0,"cache_read_input_tokens":24000,"output_tokens":300,"requests":2,"cost":0.004,"currency":"USD","unpriced":0}}

                              """;

        var reply = await LouisAgentClient.ReadStreamAsync(Bytes(stream));

        Assert.That(reply.Error, Is.EqualTo("HttpRequestException: 529 overloaded"));
        Assert.That(reply.Usage, Is.EqualTo(new UsageTotal("louis-agent", 2, 1_200, 0, 24_000, 300, 0.004m, 0)));
    }

    /// <summary>An API that records no usage leaves it empty, and the orchestrator works as before.</summary>
    [Test]
    public async Task ReadStream_NoUsage_IsNull()
    {
        var reply = await LouisAgentClient.ReadStreamAsync(Bytes(Stream));

        Assert.That(reply.Usage, Is.Null);
    }

    /// <summary>Text blocks split by a tool call stay apart, never run together.</summary>
    [Test]
    public async Task ReadStream_KeepsTextBlocksApart()
    {
        const string stream = """
                              event: content_block_start
                              data: {"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}

                              event: content_block_delta
                              data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Let me look at the code:"}}

                              event: content_block_start
                              data: {"type":"content_block_start","index":1,"content_block":{"type":"tool_use","id":"t1","name":"ReadWorkspaceFile","input":{}}}

                              event: content_block_start
                              data: {"type":"content_block_start","index":2,"content_block":{"type":"text","text":""}}

                              event: content_block_delta
                              data: {"type":"content_block_delta","index":2,"delta":{"type":"text_delta","text":"Now the fix."}}

                              """;
        var reply = await LouisAgentClient.ReadStreamAsync(Bytes(stream));

        Assert.That(reply.Text, Is.EqualTo("Let me look at the code:\n\nNow the fix."));
    }

    /// <summary>An error event ends up as the reply's error.</summary>
    [Test]
    public async Task ReadStream_ReportsAnErrorEvent()
    {
        const string stream = """
                              event: error
                              data: {"type":"error","error":{"type":"api_error","message":"prompt is too long"}}

                              """;
        var reply = await LouisAgentClient.ReadStreamAsync(Bytes(stream));

        Assert.That(reply.Error, Is.EqualTo("prompt is too long"));
    }

    /// <summary>The FIX-RESULT line at the end of a streamed answer reads into its fields.</summary>
    [Test]
    public async Task ParseFixResult_ReadsTheStreamedResultLine()
    {
        var reply = await LouisAgentClient.ReadStreamAsync(Bytes(Stream));
        var result = LouisAgentClient.ParseFixResult(reply.Text);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Status, Is.EqualTo("fixed"));
        Assert.That(result.Commit, Is.EqualTo("abc1234"));
        Assert.That(result.Branch, Is.EqualTo("fix/x"));
    }

    /// <summary>With more than one line the last counts, and backticks around the JSON are fine.</summary>
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

    /// <summary>
    /// A summary with unescaped quotes still reads (FX8, from the full demo of 2026-10-11): valid in every way but the
    /// quotes around "Cher", so not valid JSON, and the orchestrator read "none" and asked louis-agent to continue
    /// three times.
    /// </summary>
    [Test]
    public void ParseFixResult_UnescapedQuotesInTheSummary_StillReadsTheResult()
    {
        const string text = """
                            Committed the fix.
                            FIX-RESULT: {"status":"fixed","commit":"13eb2b0","branch":"fix/customer-initials-9c8637c0128d","tests":"16/16 passed","summary":"Single-word names now return one initial and multi-word names use the first and last word, so order 1012 ("Cher") no longer throws IndexOutOfRangeException."}
                            """;

        FixResult? result = LouisAgentClient.ParseFixResult(text);

        Assert.That(result, Is.EqualTo(new FixResult("fixed", "13eb2b0", "fix/customer-initials-9c8637c0128d",
            "16/16 passed",
            "Single-word names now return one initial and multi-word names use the first and last word, so order 1012 (\"Cher\") no longer throws IndexOutOfRangeException.",
            null)));
    }

    /// <summary>A failure's reason with unescaped quotes still reads.</summary>
    [Test]
    public void ParseFixResult_UnescapedQuotesInAReason_StillReadsTheFailure()
    {
        const string text =
            """FIX-RESULT: {"status":"failed","reason":"the "Cher" case needs a product decision","tests":"15/16 passed"}""";

        FixResult? result = LouisAgentClient.ParseFixResult(text);

        Assert.That((result!.Status, result.Reason, result.Tests),
            Is.EqualTo(("failed", "the \"Cher\" case needs a product decision", "15/16 passed")));
    }

    /// <summary>No line, a line that isn't JSON at all, or one without a status: no result.</summary>
    [TestCase("All done, no result line.")]
    [TestCase("FIX-RESULT: {not json}")]
    [TestCase("FIX-RESULT: {\"commit\":\"abc1234\"}")]
    public void ParseFixResult_WithoutAValidLine_IsNull(string text) =>
        Assert.That(LouisAgentClient.ParseFixResult(text), Is.Null);

    /// <summary>
    /// A request the API refuses is reported with its status and body. The texts that reach the models moved into
    /// OrchestratorToolMessage (CL3); this and the next pin the ones no other test reads.
    /// </summary>
    [Test]
    public async Task AFailedRequest_IsReportedWithItsStatusAndBody()
    {
        using var http = new HttpClient(new Failing()) { BaseAddress = new Uri("http://louis-agent/") };

        AgentReply reply = await new LouisAgentClient(http, null).SendAsync("sess_1", "Fix it");

        Assert.That(reply.Error, Is.EqualTo("HTTP 404: no session sess_1"));
    }

    /// <summary>The follow-up after a fix with no result asks for the FIX-RESULT line.</summary>
    [Test]
    public void TheFollowUp_AsksForTheFixResult()
    {
        Assert.That(LouisAgent.Orchestrator.Tools.ServiceTools.ContinueMessage,
            Is.EqualTo("Continue the fix from where you stopped. Finish with the FIX-RESULT line."));
    }

    /// <summary>An API that has no such session.</summary>
    private sealed class Failing : HttpMessageHandler
    {
        /// <summary>Every request gets a 404 naming the session.</summary>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)
            {
                Content = new StringContent("no session sess_1")
            });
    }
}
