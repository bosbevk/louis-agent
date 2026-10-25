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

    // The model's answers replayed (F18 step 4e). A miss never calls the model; these say what changed in the request.
    [Description("No recorded model answer for this request in trial '{Trial}': {Change}. The model was not called.")]
    ModelReplayMiss,

    [Description("Trial '{Trial}' has no recorded model answers: it was recorded before step 4e, or is a different run. The model was not called.")]
    ModelReplayNoAnswers,

    [Description("the model changed (recorded '{Recorded}', now '{Requested}')")]
    ReplayChangedModel,

    [Description("the system prompt changed")]
    ReplayChangedSystem,

    [Description("the tool '{Name}' changed")]
    ReplayChangedTool,

    [Description("the tool '{Name}' was added")]
    ReplayAddedTool,

    [Description("the tool '{Name}' is gone")]
    ReplayGoneTool,

    [Description("the tools came in another order")]
    ReplayReorderedTools,

    [Description("the settings changed")]
    ReplayChangedSettings,

    [Description("message {Number} ({Role}) changed")]
    ReplayChangedMessage,

    [Description("message {Number} ({Role}) was added")]
    ReplayAddedMessage,

    [Description("message {Number} ({Role}) is gone")]
    ReplayGoneMessage,

    [Description("nothing in the request changed, but its recorded answer is missing or unreadable")]
    ReplayNothingChanged,

    [Description("Model replay needs the streaming updates coalesced into one response, as the cache stores them.")]
    ModelReplayNeedsCoalescing,
}
