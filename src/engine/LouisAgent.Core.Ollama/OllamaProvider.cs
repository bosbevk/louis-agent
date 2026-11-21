using LouisAgent.Core.Config;
using LouisAgent.Core.Llm.Providers;
using Microsoft.Extensions.AI;
using OllamaSharp;

namespace LouisAgent.Core.Ollama;

/// <summary>
/// A local model through Ollama: the client at <c>LLM_ENDPOINT</c>, or Ollama's own port on this machine, with a
/// timeout long enough for a small GPU. It reports usage in the standard fields and has no prompt cache.
/// </summary>
/// <remarks>
/// The Ollama Strategy for core's <see cref="ILlmProvider"/> (ADR 0003), in its own library (CL57, ADR 0092).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0003-provider-strategies.md">ADR 0003: provider strategies</seealso>
public sealed class OllamaProvider : ILlmProvider
{
    /// <summary>The one instance: it holds nothing of its own.</summary>
    public static readonly OllamaProvider Instance = new();

    /// <summary>
    /// A local model on a small GPU writes about five tokens a second, so one reply can take minutes; HttpClient's
    /// default 100 seconds cancelled replies Ollama was still writing.
    /// </summary>
    public static readonly TimeSpan Timeout = TimeSpan.FromMinutes(10);

    private OllamaProvider()
    {
    }

    public string Name => LlmOptions.Ollama;

    public IChatClient Create(LlmOptions options) => new OllamaApiClient(CreateHttpClient(options), options.Model);

    /// <summary>The HTTP client Ollama's talks through: the endpoint named, else localhost, and the long timeout.</summary>
    public static HttpClient CreateHttpClient(LlmOptions options) => new()
    {
        BaseAddress =
            new Uri(string.IsNullOrWhiteSpace(options.Endpoint) ? "http://localhost:11434" : options.Endpoint),
        Timeout = Timeout,
    };
}
