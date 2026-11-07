using LouisAgent.Core.Config;
using LouisAgent.Core.Llm;
using LouisAgent.Core.Llm.Caching;
using LouisAgent.Core.Llm.Providers;
using LouisAgent.Core.Llm.UsageMapping;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Llm;

/// <summary>
/// The factory as a registry of the providers a host gave (CL57): it picks by LLM_PROVIDER's name without case, says
/// which names it knows when asked for another, and with none says it knows none; a mistyped cache setting fails on
/// every provider; caching off gives no caching whatever the provider offers.
/// </summary>
public class LlmClientFactoryTests
{
    private sealed class FakeProvider(string name, IPromptCachingStrategy? caching = null) : ILlmProvider
    {
        public string Name => name;

        public IChatClient Create(LlmOptions options) => new FakeChatClient();

        public IPromptCachingStrategy CreatePromptCaching(LlmOptions options) => caching ?? NoPromptCaching.Instance;
    }

    private sealed class Markers : IPromptCachingStrategy
    {
        public (IEnumerable<ChatMessage> Messages, ChatOptions? Options) Prepare(IEnumerable<ChatMessage> messages,
            ChatOptions? options) => (messages, options);
    }

    private static LlmOptions Options(string provider, string? cache = null, string? ttl = null) =>
        new()
        {
            Provider = provider,
            Model = "m",
            Cache = cache,
            CacheTtl = ttl
        };

    [Test]
    public void ItPicksByName_WithoutCase()
    {
        var factory = new LlmClientFactory([new FakeProvider("local"), new FakeProvider("cloud", new Markers())]);

        Assert.That(factory.CreatePromptCaching(Options("CLOUD")), Is.TypeOf<Markers>());
    }

    [Test]
    public void AnotherName_ListsTheOnesItKnows()
    {
        var factory = new LlmClientFactory([new FakeProvider("local"), new FakeProvider("cloud")]);

        var ex = Assert.Throws<ArgumentException>(() => factory.Create(Options("other")));

        Assert.That(ex!.Message, Does.Contain("other").And.Contain("local, cloud"));
    }

    [Test]
    public void NoProviders_MeansEveryNameIsUnknown()
    {
        var factory = new LlmClientFactory([]);

        Assert.That(() => factory.Create(Options("anthropic")), Throws.ArgumentException);
    }

    [Test]
    public void Create_WrapsTheProvidersClient_InTheCachingClient()
    {
        IChatClient client = new LlmClientFactory([new FakeProvider("local")]).Create(Options("local"));

        Assert.That(client.GetService<PromptCachingChatClient>()?.Strategy, Is.SameAs(NoPromptCaching.Instance));
    }

    [Test]
    public void NoModel_IsRefused_BeforeTheProviderIsAsked() =>
        Assert.That(() => new LlmClientFactory([new FakeProvider("local")]).Create(new LlmOptions
        {
            Provider = "local",
            Model = ""
        }), Throws.ArgumentException);

    [Test]
    public void CachingOff_GivesNoCaching_WhateverTheProviderOffers() =>
        Assert.That(new LlmClientFactory([new FakeProvider("cloud", new Markers())])
            .CreatePromptCaching(Options("cloud", cache: "off")), Is.SameAs(NoPromptCaching.Instance));

    [TestCase("maybe", null)]
    [TestCase(null, "10m")]
    public void AMistypedCacheSetting_FailsOnEveryProvider(string? cache, string? ttl) =>
        // Even where nothing is cached, so the typo shows up before someone switches provider and wonders why.
        Assert.That(
            () => new LlmClientFactory([new FakeProvider("local")]).CreatePromptCaching(Options("local", cache, ttl)),
            Throws.ArgumentException);

    [Test]
    public void TheMapper_IsTheProviders_OrTheStandardOne() =>
        Assert.That(new LlmClientFactory([new FakeProvider("local")]).CreateUsageMapper(Options("local")),
            Is.TypeOf<StandardUsageMapper>());
}
