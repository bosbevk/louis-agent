using LouisAgent.Composition.Messages;
using LouisAgent.Core.Llm.Messages.Holes.Providers;
using LouisAgent.Core.Llm.Messages.Holes.ReplayMessages;
using LouisAgent.Core.Llm.Messages.Holes.ReplayMisses;
using LouisAgent.Core.Llm.Messages.Holes.ReplayTools;
using System.ComponentModel;

namespace LouisAgent.Core.Llm.Messages;

/// <summary>
/// Exception texts for the LLM provider area: what is wrong and how to fix it, and why a replayed request found no
/// recorded answer. Each member is named for what happened, and its [Description] is the text, with named holes filled
/// by MessageText.Format from the record its [Holes] names (src/docs/CLAUDE.md, *Messages and logging*).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0119-declare-name-then-pass.md">ADR 0119: named types, declared, then passed</seealso>
public enum LlmErrorMessage
{
    [Description("LLM_MODEL is required.")]
    LlmModelRequired,

    [Description("Unknown LLM_PROVIDER '{Provider}'. Supported: {Supported}.")]
    [Holes(typeof(UnknownProviderHoles))]
    UnknownProvider,

    // The model's answers replayed (F18 step 4e). A miss never calls the model; these say what changed in the request.
    [Description("No recorded model answer for this request in trial '{Trial}': {Change}. The model was not called.")]
    [Holes(typeof(ModelReplayMissHoles))]
    ModelReplayMiss,

    [Description(
        "Trial '{Trial}' has no recorded model answers: it was recorded before step 4e, or is a different run. The " +
        "model was not called.")]
    [Holes(typeof(ModelReplayNoAnswersHoles))]
    ModelReplayNoAnswers,

    [Description("the model changed (recorded '{Recorded}', now '{Requested}')")]
    [Holes(typeof(ReplayChangedModelHoles))]
    ReplayChangedModel,

    [Description("the system prompt changed")]
    ReplayChangedSystem,

    [Description("the tool '{Name}' changed")]
    [Holes(typeof(ReplayChangedToolHoles))]
    ReplayChangedTool,

    [Description("the tool '{Name}' was added")]
    [Holes(typeof(ReplayAddedToolHoles))]
    ReplayAddedTool,

    [Description("the tool '{Name}' is gone")]
    [Holes(typeof(ReplayGoneToolHoles))]
    ReplayGoneTool,

    [Description("the tools came in another order")]
    ReplayReorderedTools,

    [Description("the settings changed")]
    ReplayChangedSettings,

    [Description("message {Number} ({Role}) changed")]
    [Holes(typeof(ReplayChangedMessageHoles))]
    ReplayChangedMessage,

    [Description("message {Number} ({Role}) was added")]
    [Holes(typeof(ReplayAddedMessageHoles))]
    ReplayAddedMessage,

    [Description("message {Number} ({Role}) is gone")]
    [Holes(typeof(ReplayGoneMessageHoles))]
    ReplayGoneMessage,

    [Description("nothing in the request changed, but its recorded answer is missing or unreadable")]
    ReplayNothingChanged,

    [Description("Model replay needs the streaming updates coalesced into one response, as the cache stores them.")]
    ModelReplayNeedsCoalescing,
}
