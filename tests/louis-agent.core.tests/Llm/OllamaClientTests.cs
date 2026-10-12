using LouisAgent.Core.Config;
using LouisAgent.Core.Llm;

namespace LouisAgent.Core.Tests.Llm;

// HttpClient's default 100 seconds cancelled replies a local model was still writing.
public class OllamaClientTests
{
    [Test]
    public void TheHttpClient_WaitsLongerThanTheDefault100Seconds()
    {
        using var http = LlmClientFactory.CreateOllamaHttpClient(new LlmOptions { Provider = LlmOptions.Ollama, Model = "m" });

        Assert.That(http.Timeout, Is.EqualTo(LlmClientFactory.OllamaTimeout));
        Assert.That(http.Timeout, Is.GreaterThan(TimeSpan.FromSeconds(100)));
    }

    [TestCase(null, "http://localhost:11434/")]
    [TestCase("http://host.docker.internal:11434", "http://host.docker.internal:11434/")]
    public void TheHttpClient_TalksToTheEndpoint_OrLocalhost(string? endpoint, string expected)
    {
        using var http = LlmClientFactory.CreateOllamaHttpClient(new LlmOptions { Provider = LlmOptions.Ollama, Model = "m", Endpoint = endpoint });

        Assert.That(http.BaseAddress, Is.EqualTo(new Uri(expected)));
    }
}
