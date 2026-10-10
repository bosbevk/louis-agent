using System.Text.Json;
using louis_agent.core.usage;
using Microsoft.Extensions.AI;

namespace louis_agent.core.tests;

// F1-S1: one record per request, streaming or not. F1-S4: no message text in a record.
public class UsageRecordingChatClientTests
{
    private static ChatResponse Reply(string text, UsageDetails? usage = null) =>
        new(new ChatMessage(ChatRole.Assistant, text))
        {
            Usage = usage,
            ModelId = "claude-haiku-4-5-20251001",
            FinishReason = ChatFinishReason.Stop,
        };

    [Test]
    public async Task GetResponse_WritesOneRecordWithCountsAndScope()
    {
        var sink = new ListUsageSink();
        var usage = new UsageDetails { InputTokenCount = 10_266, CachedInputTokenCount = 10_227, OutputTokenCount = 206 };
        using var client = new UsageRecordingChatClient(new FakeChatClient(_ => Reply("Hello.", usage)), sink);

        using (UsageScope.Begin(UsagePurpose.Summary))
            await client.GetResponseAsync("Say hello.");

        Assert.That(sink.Records, Has.Count.EqualTo(1));
        UsageRecord record = sink.Records[0];
        Assert.That(record.Tokens, Is.EqualTo(new UsageTokens(39, null, 10_227, 206, null)));
        Assert.That(record.Round, Is.EqualTo(1));
        Assert.That(record.Purpose, Is.EqualTo(UsagePurpose.Summary));
        Assert.That(record.Model, Is.EqualTo("claude-haiku-4-5-20251001"));
        Assert.That(record.Stop, Is.EqualTo("stop"));
    }

    [Test]
    public async Task GetResponse_TwoRequestsInOneScope_AreRounds1And2()
    {
        var sink = new ListUsageSink();
        using var client = new UsageRecordingChatClient(new FakeChatClient(_ => Reply("one"), _ => Reply("two")), sink);

        using (UsageScope.Begin())
        {
            await client.GetResponseAsync("first");
            await client.GetResponseAsync("second");
        }

        Assert.That(sink.Records.Select(r => r.Round), Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task GetResponse_NoScope_RecordsRound1AsTurn()
    {
        var sink = new ListUsageSink();
        using var client = new UsageRecordingChatClient(new FakeChatClient(_ => Reply("hi")), sink);

        await client.GetResponseAsync("hi");

        Assert.That(sink.Records.Single().Round, Is.EqualTo(1));
        Assert.That(sink.Records.Single().Purpose, Is.EqualTo(UsagePurpose.Turn));
    }

    [Test]
    public async Task GetResponse_NoUsageReported_CountsNullNotZero()
    {
        var sink = new ListUsageSink();
        using var client = new UsageRecordingChatClient(new FakeChatClient(_ => Reply("hi", usage: null)), sink);

        await client.GetResponseAsync("hi");

        Assert.That(sink.Records.Single().Tokens, Is.EqualTo(new UsageTokens(null, null, null, null, null)));
    }

    [Test]
    public async Task GetResponse_ResponseWithoutModel_UsesDefaultModel()
    {
        var sink = new ListUsageSink();
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "hi"));
        using var client = new UsageRecordingChatClient(new FakeChatClient(_ => response), sink, defaultModel: "qwen2.5-coder");

        await client.GetResponseAsync("hi");

        Assert.That(sink.Records.Single().Model, Is.EqualTo("qwen2.5-coder"));
    }

    [Test]
    public async Task GetStreamingResponse_UsageInLastUpdate_WritesOneRecordWhenStreamEnds()
    {
        // FakeChatClient streams each content as an update and ends with the finish reason, so the UsageContent arrives
        // near the end, as Anthropic's does.
        var sink = new ListUsageSink();
        var usage = new UsageDetails { InputTokenCount = 10_266, CachedInputTokenCount = 10_227, OutputTokenCount = 137 };
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, [new TextContent("Hello!"), new UsageContent(usage)]));
        using var client = new UsageRecordingChatClient(new FakeChatClient(_ => response), sink, defaultModel: "claude-haiku-4-5-20251001");

        int recordsWhileStreaming = 0;
        await foreach (var _ in client.GetStreamingResponseAsync("Say hello."))
            recordsWhileStreaming = Math.Max(recordsWhileStreaming, sink.Records.Count);

        Assert.That(recordsWhileStreaming, Is.Zero);
        UsageRecord record = sink.Records.Single();
        Assert.That(record.Tokens, Is.EqualTo(new UsageTokens(39, null, 10_227, 137, null)));
        Assert.That(record.Stop, Is.EqualTo("stop"));
        Assert.That(record.Model, Is.EqualTo("claude-haiku-4-5-20251001"));
    }

    [Test]
    public async Task GetStreamingResponse_PassesEveryUpdateThroughUnchanged()
    {
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, [new TextContent("a"), new TextContent("b")]));
        var inner = new FakeChatClient(_ => response, _ => response);
        using var client = new UsageRecordingChatClient(inner, new ListUsageSink());

        var direct = await inner.GetStreamingResponseAsync("x").ToListAsync();
        var recorded = await client.GetStreamingResponseAsync("x").ToListAsync();

        Assert.That(recorded.Select(u => u.Text), Is.EqualTo(direct.Select(u => u.Text)));
        Assert.That(recorded.Select(u => u.FinishReason), Is.EqualTo(direct.Select(u => u.FinishReason)));
    }

    [Test]
    public async Task GetStreamingResponse_TwoRequestsInOneScope_AreRounds1And2()
    {
        var sink = new ListUsageSink();
        using var client = new UsageRecordingChatClient(new FakeChatClient(_ => Reply("one"), _ => Reply("two")), sink);

        using (UsageScope.Begin())
        {
            await client.GetStreamingResponseAsync("first").ToListAsync();
            await client.GetStreamingResponseAsync("second").ToListAsync();
        }

        Assert.That(sink.Records.Select(r => r.Round), Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task GetStreamingResponse_CallerStopsEarly_RecordsWhatWasReportedAsCancelled()
    {
        // A host that stops reading (the user pressed stop) disposes the stream before the provider finishes it.
        var sink = new ListUsageSink();
        var usage = new UsageDetails { InputTokenCount = 500, OutputTokenCount = 20 };
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, [new UsageContent(usage), new TextContent("partial")]));
        using var client = new UsageRecordingChatClient(new FakeChatClient(_ => response), sink);

        await foreach (var update in client.GetStreamingResponseAsync("hi"))
            if (update.Contents.OfType<UsageContent>().Any()) break;

        UsageRecord record = sink.Records.Single();
        Assert.That(record.Stop, Is.EqualTo(UsageRecordingChatClient.Cancelled));
        Assert.That(record.Tokens.Input, Is.EqualTo(500));
    }

    [Test]
    public void GetStreamingResponse_TokenCancelled_RecordsCancelled()
    {
        var sink = new ListUsageSink();
        using var client = new UsageRecordingChatClient(new FailingStreamClient(new OperationCanceledException()), sink);

        Assert.ThrowsAsync<OperationCanceledException>(async () => await client.GetStreamingResponseAsync("hi").ToListAsync());

        Assert.That(sink.Records.Single().Stop, Is.EqualTo(UsageRecordingChatClient.Cancelled));
    }

    [Test]
    public void GetStreamingResponse_ProviderError_NotRecorded()
    {
        // Same as a failed non-streaming call: the exception propagates and no record is written.
        var sink = new ListUsageSink();
        using var client = new UsageRecordingChatClient(new FailingStreamClient(new HttpRequestException("529 overloaded")), sink);

        Assert.ThrowsAsync<HttpRequestException>(async () => await client.GetStreamingResponseAsync("hi").ToListAsync());

        Assert.That(sink.Records, Is.Empty);
    }

    [Test]
    public async Task Record_ContainsNoMessageText()
    {
        const string marker = "SECRET-PROMPT-MARKER-7f3a";
        var sink = new ListUsageSink();
        using var client = new UsageRecordingChatClient(new FakeChatClient(_ => Reply($"echo {marker}")), sink);

        await client.GetResponseAsync($"Repeat {marker}");

        Assert.That(JsonSerializer.Serialize(sink.Records.Single()), Does.Not.Contain(marker));
    }
}

/// <summary>Streams one text update, then throws the given exception, as a provider does when a stream breaks.</summary>
internal sealed class FailingStreamClient(Exception exception) : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        throw exception;

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new ChatResponseUpdate(ChatRole.Assistant, "partial");
        await Task.Yield();
        throw exception;
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }
}

/// <summary>Collects records in memory for assertions.</summary>
internal sealed class ListUsageSink : IUsageSink
{
    public List<UsageRecord> Records { get; } = [];

    public void Record(UsageRecord record) => Records.Add(record);
}
