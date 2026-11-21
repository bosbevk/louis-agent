using System.ComponentModel;

namespace LouisAgent.Core.OpenAi.Messages;

/// <summary>Exception texts for the OpenAI-compatible provider: what setting is missing and how to fix it.</summary>
public enum OpenAiErrorMessage
{
    [Description("LLM_ENDPOINT is required for provider 'openai-compatible'.")]
    EndpointRequired,
}
