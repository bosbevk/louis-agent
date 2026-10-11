using System.Net;
using System.Text.Json;
using Anthropic;
using Anthropic.Core;
using LouisAgent.Core.Usage;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests;

// F4 step 6: cache writes split by lifetime reach the ledger, from the real Anthropic adapter (a captured reply, no
// model call), streaming and not. UsageDetails has only the total; the split comes from the SDK's own usage.
public class CacheWriteLifetimeTests
{
    private const string Split = ""","cache_creation":{"ephemeral_5m_input_tokens":148,"ephemeral_1h_input_tokens":100}""";

    private static string Usage(string split = Split) =>
        $$"""
        "usage":{"input_tokens":10,"output_tokens":2,"cache_creation_input_tokens":248,"cache_read_input_tokens":0{{split}}}
        """;

    private static string Reply(string split = Split) =>
        $$$"""
        {"id":"msg_1","type":"message","role":"assistant","model":"claude-haiku-5-5",
         "content":[{"type":"text","text":"ok"}],"stop_reason":"end_turn","stop_sequence":null,{{{Usage(split)}}}}
        """;

    // The stream as the API sends it: the split only in message_start, the closing message_delta has the total.
    private static readonly string Stream = string.Concat(
        Event("message_start", $$$"""{"type":"message_start","message":{"id":"msg_1","type":"message","role":"assistant","model":"claude-haiku-5-5","content":[],"stop_reason":null,"stop_sequence":null,{{{Usage()}}}}}"""),
        Event("content_block_start", """{"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}"""),
        Event("content_block_delta", """{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"ok"}}"""),
        Event("content_block_stop", """{"type":"content_block_stop","index":0}"""),
        Event("message_delta", """{"type":"message_delta","delta":{"stop_reason":"end_turn","stop_sequence":null},"usage":{"input_tokens":10,"output_tokens":2,"cache_creation_input_tokens":248,"cache_read_input_tokens":0}}"""),
        Event("message_stop", """{"type":"message_stop"}"""));

    private static string Event(string name, string json) => $"event: {name}\ndata: {json.ReplaceLineEndings("")}\n\n";

    private static (IChatClient Client, ListUsageSink Sink) Recorder(string body, string mediaType, IUsageMapper mapper)
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, mediaType),
        });
        IChatClient adapter = new AnthropicClient(new ClientOptions { ApiKey = "test", HttpClient = new HttpClient(handler) })
            .AsIChatClient("claude-haiku-5-5", 1024);
        var sink = new ListUsageSink();
        return (new UsageRecordingChatClient(adapter, sink, mapper: mapper), sink);
    }

    [Test]
    public async Task NotStreaming_TheSplitComesFromTheResponse()
    {
        var (client, sink) = Recorder(Reply(), "application/json", new AnthropicUsageMapper());

        await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        UsageTokens tokens = sink.Records.Single().Tokens;
        Assert.That((tokens.Input, tokens.CacheWrite, tokens.CacheWrite1h), Is.EqualTo((10L, 248L, 100L)));
    }

    [Test]
    public async Task Streaming_TheSplitComesFromMessageStart()
    {
        var (client, sink) = Recorder(Stream, "text/event-stream", new AnthropicUsageMapper());

        await foreach (var _ in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")])) { }

        UsageTokens tokens = sink.Records.Single().Tokens;
        Assert.That((tokens.Input, tokens.CacheWrite, tokens.CacheWrite1h), Is.EqualTo((10L, 248L, 100L)));
    }

    [Test]
    public async Task AReplyWithoutTheSplit_LeavesItNull()
    {
        string noSplit = Reply(split: "");
        Assert.That(noSplit, Does.Not.Contain("ephemeral_1h"), "the test reply has no split");
        var (client, sink) = Recorder(noSplit, "application/json", new AnthropicUsageMapper());

        await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        Assert.That(sink.Records.Single().Tokens.CacheWrite1h, Is.Null);
    }

    [Test]
    public async Task NoWritesReported_LeavesTheSplitNullToo()
    {
        // An uncached request (the demo's triage): the API still sends cache_creation with zeros, but the adapter reports
        // no cache_creation_input_tokens, so cache_write is null; its split must be null as well, not 0 (F1).
        string uncached = Reply(split: ""","cache_creation":{"ephemeral_5m_input_tokens":0,"ephemeral_1h_input_tokens":0}""")
            .Replace("\"cache_creation_input_tokens\":248", "\"cache_creation_input_tokens\":0");
        var (client, sink) = Recorder(uncached, "application/json", new AnthropicUsageMapper());

        await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        UsageTokens tokens = sink.Records.Single().Tokens;
        Assert.That((tokens.CacheWrite, tokens.CacheWrite1h), Is.EqualTo(((long?)null, (long?)null)));
    }

    [Test]
    public async Task TheStandardMapper_KeepsNothing()
    {
        var (client, sink) = Recorder(Stream, "text/event-stream", new StandardUsageMapper());

        await foreach (var _ in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")])) { }

        Assert.That(sink.Records.Single().Tokens.CacheWrite1h, Is.Null);
    }

    [Test]
    public void TheLedger_WritesTheSplit_AndReadsOldLinesWithoutIt()
    {
        string written = JsonSerializer.Serialize(new UsageTokens(10, 248, 0, 2, null, CacheWrite1h: 100), JsonlUsageSink.Json);
        Assert.That(written, Does.Contain("\"cache_write_1h\":100"));

        var old = JsonSerializer.Deserialize<UsageTokens>("""{"input":10,"cache_write":248,"cache_read":0,"output":2,"reasoning":null}""", JsonlUsageSink.Json);
        Assert.That(old, Is.EqualTo(new UsageTokens(10, 248, 0, 2, null)));
    }
}
