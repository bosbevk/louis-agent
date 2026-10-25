namespace LouisAgent.Core.Llm.Messages;

using System.ComponentModel;

// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Exception texts for the LLM provider area: what is wrong and how to fix it.</summary>
public enum LlmErrorMessage
{
    [Description("LLM_MODEL is required.")]
    LlmModelRequired,

    [Description("Unknown LLM_PROVIDER '{Provider}'. Supported: {Supported}.")]
    UnknownProvider,

    [Description("ANTHROPIC_API_KEY (or LLM_API_KEY) is required for provider 'anthropic'.")]
    AnthropicKeyRequired,

    [Description("LLM_ENDPOINT is required for provider 'openai-compatible'.")]
    OpenAiEndpointRequired,
}
