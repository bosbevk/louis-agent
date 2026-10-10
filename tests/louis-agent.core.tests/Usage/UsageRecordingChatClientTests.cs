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
    public void GetStreamingResponse_UsageInLastUpdate_WritesOneRecordWhenStreamEnds()
    {
        // FakeChatClient streams the response's contents, so put a UsageContent in the response message.
        Assert.Ignore("TODO F1 step 4");
    }

    [Test]
    public void GetStreamingResponse_Cancelled_RecordsWithStopCancelled()
    {
        Assert.Ignore("TODO F1 step 4");
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

/// <summary>Collects records in memory for assertions.</summary>
internal sealed class ListUsageSink : IUsageSink
{
    public List<UsageRecord> Records { get; } = [];

    public void Record(UsageRecord record) => Records.Add(record);
}
