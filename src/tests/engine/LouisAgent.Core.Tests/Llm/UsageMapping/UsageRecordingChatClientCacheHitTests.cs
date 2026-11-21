using LouisAgent.Core.Llm.UsageMapping;
using LouisAgent.Core.Usage.Records;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Llm.UsageMapping;

// F18 step 4e.2: a replayed answer is a known 0 (ADR 0007): the recorder sees the hit tag, on the response or on any update of a stream,
// and writes zero tokens with cache_hit; an untagged request is recorded exactly as before.
public class UsageRecordingChatClientCacheHitTests
{
    private static readonly UsageDetails Recorded = new() { InputTokenCount = 500, OutputTokenCount = 40 };

    /// <summary>A provider whose answer is tagged (or not) the way the replay client tags one; it streams one tagged update.</summary>
    private sealed class TaggedClient(bool tagged) : IChatClient
    {
        private AdditionalPropertiesDictionary? Tag => tagged ? new() { ["replay_hit"] = true } : null;

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "hi"))
            { Usage = Recorded, ModelId = "m1", AdditionalProperties = Tag });

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation]
            CancellationToken cancellationToken = default)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, "hi") { ModelId = "m1" };
            yield return new ChatResponseUpdate { AdditionalProperties = Tag, FinishReason = ChatFinishReason.Stop };
            await Task.CompletedTask;
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    private static async Task<UsageRecord> Run(bool tagged, bool streamed)
    {
        var sink = new ListUsageSink();
        using var client = new UsageRecordingChatClient(new TaggedClient(tagged), sink);
        if (streamed)
            await foreach (var _ in client.GetStreamingResponseAsync("hello"))
            {
            }
        else await client.GetResponseAsync("hello");

        return sink.Records.Single();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ATaggedAnswer_IsRecordedWithZeroTokens_AndCacheHit(bool streamed)
    {
        UsageRecord record = await Run(tagged: true, streamed);

        Assert.That(record.Tokens, Is.EqualTo(new UsageTokens(0, 0, 0, 0, 0)));
        Assert.That(record.CacheHit, Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task AnUntaggedAnswer_IsRecordedAsBefore(bool streamed)
    {
        UsageRecord record = await Run(tagged: false, streamed);

        Assert.That(record.CacheHit, Is.Null);
        Assert.That(record.Tokens.Input, Is.Not.EqualTo(0));
        Assert.That(streamed || record.Tokens.Output == 40, "a non-streamed response carries its usage");
    }
}
