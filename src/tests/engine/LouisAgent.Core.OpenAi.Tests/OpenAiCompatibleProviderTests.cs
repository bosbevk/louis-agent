using LouisAgent.Core.Config;
using LouisAgent.Core.Llm;
using LouisAgent.Core.Llm.Caching;
using LouisAgent.Core.Llm.UsageMapping;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.OpenAi.Tests;

/// <summary>
/// The OpenAI-compatible provider (CL57, out of core): it needs an endpoint and no key, reports usage in the standard
/// fields and marks no cache.
/// </summary>
public class OpenAiCompatibleProviderTests
{
    private static LlmOptions Options(string? endpoint = "http://localhost:1234/v1") =>
        new()
        {
            Provider = LlmOptions.OpenAiCompatible,
            Model = "m",
            Endpoint = endpoint
        };

    [Test]
    public void WithoutAnEndpoint_ItRefuses()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            OpenAiCompatibleProvider.Instance.Create(Options(null)));

        Assert.That(ex!.Message, Does.Contain("LLM_ENDPOINT"));
    }

    [Test]
    public void WithAnEndpointAndNoKey_ItBuilds() =>
        Assert.That(OpenAiCompatibleProvider.Instance.Create(Options()), Is.Not.Null);

    [Test]
    public void ItIsNamedForTheSetting_WithTheStandardMapper_AndNoCache()
    {
        var factory = new LlmClientFactory([OpenAiCompatibleProvider.Instance]);

        Assert.Multiple(() =>
        {
            Assert.That(OpenAiCompatibleProvider.Instance.Name, Is.EqualTo("openai-compatible"));
            Assert.That(factory.CreateUsageMapper(Options()), Is.TypeOf<StandardUsageMapper>());
            Assert.That(factory.CreatePromptCaching(Options()), Is.SameAs(NoPromptCaching.Instance));
            Assert.That(factory.Create(Options()).GetService<PromptCachingChatClient>(), Is.Not.Null);
        });
    }
}
