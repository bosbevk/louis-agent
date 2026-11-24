using LouisAgent.Core.Llm.Caching;
using LouisAgent.Core.Usage.Records;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Llm.Caching;

/// <summary>
/// Which requests are marked for the provider's prompt cache: a turn's are, and a one-off request is not, since a
/// cache write costs more and is never read back. A summary is one, and so is a tool's reading of a file (F171.1,
/// Louis's pick, "Like a summary").
/// </summary>
public class PromptCachingChatClientTests
{
    /// <summary>A provider's strategy that counts the requests it was asked to mark.</summary>
    private sealed class CountingStrategy : IPromptCachingStrategy
    {
        public int Marked { get; private set; }

        public (IEnumerable<ChatMessage> Messages, ChatOptions? Options) Prepare(IEnumerable<ChatMessage> messages,
            ChatOptions? options)
        {
            Marked++;
            return (messages, options);
        }
    }

    /// <summary>How many of one request, sent in a scope of this purpose, the strategy was asked to mark.</summary>
    private static async Task<int> MarkedIn(string purpose)
    {
        var strategy = new CountingStrategy();
        var client = new PromptCachingChatClient(
            new FakeChatClient(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok"))), strategy);

        using (UsageScope.Begin(purpose))
            await client.GetResponseAsync("Hi");

        return strategy.Marked;
    }

    /// <summary>A turn's request is marked; a summary's and a tool's are not.</summary>
    [TestCase(UsagePurpose.Turn, 1)]
    [TestCase(UsagePurpose.Summary, 0)]
    [TestCase(UsagePurpose.Tool, 0)]
    public async Task ARequest_IsMarked_OnlyWhenItWillBeRepeated(string purpose, int marked) =>
        Assert.That(await MarkedIn(purpose), Is.EqualTo(marked));
}
