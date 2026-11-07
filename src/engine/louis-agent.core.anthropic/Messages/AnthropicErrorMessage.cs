using System.ComponentModel;

namespace LouisAgent.Core.Anthropic.Messages;

/// <summary>Exception texts for the Anthropic provider: what setting is missing and how to fix it.</summary>
public enum AnthropicErrorMessage
{
    [Description("ANTHROPIC_API_KEY (or LLM_API_KEY) is required for provider 'anthropic'.")]
    KeyRequired,
}
