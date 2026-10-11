using LouisAgent.Core.Config;
using louis_agent.core.providers;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests;

// F4 step 5: LlmClientFactory picks the caching strategy once, as it picks the usage mapper.
public class PromptCachingFactoryTests
{
    private static LlmOptions Options(string provider, string? cache = null, string? ttl = null) => new()
    {
        Provider = provider, Model = "m", ApiKey = "test", Endpoint = provider == LlmOptions.OpenAiCompatible ? "http://localhost:1234/v1" : null,
        Cache = cache, CacheTtl = ttl,
    };

    [TestCase(LlmOptions.Anthropic, typeof(AnthropicPromptCaching))]
    [TestCase(LlmOptions.Ollama, typeof(NoPromptCaching))]
    [TestCase(LlmOptions.OpenAiCompatible, typeof(NoPromptCaching))]
    public void EachProvider_GetsItsStrategy(string provider, Type expected)
    {
        Assert.That(new LlmClientFactory().CreatePromptCaching(Options(provider)), Is.TypeOf(expected));
    }

    [Test]
    public void CachingOff_GivesNoPromptCaching_OnAnthropicToo()
    {
        Assert.That(new LlmClientFactory().CreatePromptCaching(Options(LlmOptions.Anthropic, cache: "off")), Is.SameAs(NoPromptCaching.Instance));
    }

    [TestCase(null, CacheLifetime.FiveMinutes)]
    [TestCase("1h", CacheLifetime.OneHour)]
    public void TheSystemMarker_TakesLlmCacheTtl(string? ttl, CacheLifetime expected)
    {
        var strategy = (AnthropicPromptCaching)new LlmClientFactory().CreatePromptCaching(Options(LlmOptions.Anthropic, ttl: ttl));

        Assert.That(strategy.PrefixLifetime, Is.EqualTo(expected));
    }

    [TestCase("maybe", null)]
    [TestCase(null, "10m")]
    public void AMistypedSetting_FailsOnEveryProvider(string? cache, string? ttl)
    {
        // Even where nothing is cached, so the typo shows up before someone switches provider and wonders why.
        Assert.That(() => new LlmClientFactory().CreatePromptCaching(Options(LlmOptions.Ollama, cache, ttl)), Throws.ArgumentException);
    }

    [TestCase(LlmOptions.Anthropic, typeof(AnthropicPromptCaching))]
    [TestCase(LlmOptions.Ollama, typeof(NoPromptCaching))]
    [TestCase(LlmOptions.OpenAiCompatible, typeof(NoPromptCaching))]
    public void Create_WrapsEveryProvidersClient_InTheCachingClient(string provider, Type expected)
    {
        IChatClient client = new LlmClientFactory().Create(Options(provider));

        var caching = client.GetService<PromptCachingChatClient>();
        Assert.That(caching, Is.Not.Null);
        Assert.That(caching!.Strategy, Is.TypeOf(expected));
    }
}