using LouisAgent.Core.Anthropic.Messages;
using LouisAgent.Core.Config;
using LouisAgent.Core.Llm;
using LouisAgent.Core.Llm.Caching;
using LouisAgent.Core.Llm.UsageMapping;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Anthropic.Tests;

/// <summary>
/// The Anthropic provider as the registry sees it (CL57): named for LLM_PROVIDER, refusing to build without a key,
/// its own usage mapper, its markers with the lifetime LLM_CACHE_TTL names, and nothing when caching is off.
/// </summary>
public class AnthropicProviderTests
{
    private static LlmOptions Options(string? cache = null, string? ttl = null) => new()
    {
        Provider = LlmOptions.Anthropic,
        Model = "claude-haiku-5-5",
        ApiKey = "test",
        Cache = cache,
        CacheTtl = ttl,
    };

    private static LlmClientFactory Factory() => new([AnthropicProvider.Instance]);

    [Test]
    public void ItIsNamedForTheSetting() => Assert.That(AnthropicProvider.Instance.Name, Is.EqualTo("anthropic"));

    [Test]
    public void WithoutAKey_ItRefuses()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            AnthropicProvider.Instance.Create(new LlmOptions
            {
                Provider = LlmOptions.Anthropic,
                Model = "m"
            }));

        Assert.That(ex!.Message, Does.Contain("ANTHROPIC_API_KEY"));
    }

    [Test]
    public void ItReadsCacheWrites_ThroughItsOwnMapper() =>
        Assert.That(Factory().CreateUsageMapper(Options()), Is.TypeOf<AnthropicUsageMapper>());

    [TestCase(null, CacheLifetime.FiveMinutes)]
    [TestCase("1h", CacheLifetime.OneHour)]
    public void TheSystemMarker_TakesLlmCacheTtl(string? ttl, CacheLifetime expected)
    {
        var strategy = (AnthropicPromptCaching)Factory().CreatePromptCaching(Options(ttl: ttl));

        Assert.That(strategy.PrefixLifetime, Is.EqualTo(expected));
    }

    [Test]
    public void CachingOff_GivesNoPromptCaching() =>
        Assert.That(Factory().CreatePromptCaching(Options(cache: "off")), Is.SameAs(NoPromptCaching.Instance));

    [Test]
    public void Create_WrapsTheClient_InTheCachingClient()
    {
        IChatClient client = Factory().Create(Options());

        Assert.That(client.GetService<PromptCachingChatClient>()?.Strategy, Is.TypeOf<AnthropicPromptCaching>());
    }

    [Test]
    public void TheKeyMessage_NamesTheSetting() =>
        Assert.That(MessageEnumText.Of(AnthropicErrorMessage.KeyRequired), Does.Contain("LLM_API_KEY"));

    private static class MessageEnumText
    {
        public static string Of(Enum member) =>
            Composition.Messages.MessageText.Format(member);
    }
}
