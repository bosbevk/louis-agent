using System.ClientModel;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Config;
using LouisAgent.Core.Llm.Providers;
using LouisAgent.Core.OpenAi.Messages;
using Microsoft.Extensions.AI;
using OpenAI;

namespace LouisAgent.Core.OpenAi;

/// <summary>
/// Any server that speaks OpenAI's chat API at the endpoint you name (LM Studio, vLLM, OpenAI itself): the key is
/// optional, since a local server often wants none. It reports usage in the standard fields and marks no cache.
/// </summary>
/// <remarks>
/// The OpenAI-compatible Strategy for core's <see cref="ILlmProvider"/> (ADR 0003), in its own library (CL57, ADR 0092).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0003-provider-strategies.md">ADR 0003: provider strategies</seealso>
public sealed class OpenAiCompatibleProvider : ILlmProvider
{
    /// <summary>The one instance: it holds nothing of its own.</summary>
    public static readonly OpenAiCompatibleProvider Instance = new();

    private OpenAiCompatibleProvider()
    {
    }

    public string Name => LlmOptions.OpenAiCompatible;

    public IChatClient Create(LlmOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Endpoint))
            throw new InvalidOperationException(MessageText.Format(OpenAiErrorMessage.EndpointRequired));

        // Local servers often need no key, but the SDK requires a non-empty credential.
        var client = new OpenAIClient(
            new ApiKeyCredential(string.IsNullOrWhiteSpace(options.ApiKey) ? "none" : options.ApiKey),
            new OpenAIClientOptions { Endpoint = new Uri(options.Endpoint) });
        return client.GetChatClient(options.Model).AsIChatClient();
    }
}
