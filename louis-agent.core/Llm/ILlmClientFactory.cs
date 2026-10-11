namespace LouisAgent.Core.Llm;

using System.ClientModel;
using Anthropic;
using Anthropic.Core;
using Anthropic.Models.Messages;
using LouisAgent.Core.Config;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;

public interface ILlmClientFactory
{
    IChatClient Create(LlmOptions options);

    /// <summary>How this provider's usage maps to the ledger's token kinds.</summary>
    IUsageMapper CreateUsageMapper(LlmOptions options) => new StandardUsageMapper();

    /// <summary>How this provider marks requests for its prompt cache (F4).</summary>
    IPromptCachingStrategy CreatePromptCaching(LlmOptions options) => NoPromptCaching.Instance;
}
