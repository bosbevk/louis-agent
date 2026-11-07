using LouisAgent.Core.Config;
using LouisAgent.Core.Llm;
using LouisAgent.Core.Llm.Caching;
using LouisAgent.Core.Llm.UsageMapping;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Ollama.Tests;

/// <summary>
/// The Ollama provider (CL57, out of core): HttpClient's default 100 seconds cancelled replies a local model was still
/// writing; the endpoint named or localhost; the standard usage mapper and no cache.
/// </summary>
public class OllamaProviderTests
{
    private static LlmOptions Options(string? endpoint = null) =>
        new()
        {
            Provider = LlmOptions.Ollama,
            Model = "m",
            Endpoint = endpoint
        };

    [Test]
    public void TheHttpClient_WaitsLongerThanTheDefault100Seconds()
    {
        using var http = OllamaProvider.CreateHttpClient(Options());

        Assert.That(http.Timeout, Is.EqualTo(OllamaProvider.Timeout).And.GreaterThan(TimeSpan.FromSeconds(100)));
    }

    [TestCase(null, "http://localhost:11434/")]
    [TestCase("http://host.docker.internal:11434", "http://host.docker.internal:11434/")]
    public void TheHttpClient_TalksToTheEndpoint_OrLocalhost(string? endpoint, string expected)
    {
        using var http = OllamaProvider.CreateHttpClient(Options(endpoint));

        Assert.That(http.BaseAddress, Is.EqualTo(new Uri(expected)));
    }

    [Test]
    public void ItIsNamedForTheSetting_WithTheStandardMapper_AndNoCache()
    {
        var factory = new LlmClientFactory([OllamaProvider.Instance]);

        Assert.Multiple(() =>
        {
            Assert.That(OllamaProvider.Instance.Name, Is.EqualTo("ollama"));
            Assert.That(factory.CreateUsageMapper(Options()), Is.TypeOf<StandardUsageMapper>());
            Assert.That(factory.CreatePromptCaching(Options()), Is.SameAs(NoPromptCaching.Instance));
            Assert.That(factory.Create(Options()).GetService<PromptCachingChatClient>(), Is.Not.Null);
        });
    }
}
