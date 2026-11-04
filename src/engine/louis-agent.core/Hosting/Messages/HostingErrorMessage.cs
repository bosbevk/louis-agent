
using System.ComponentModel;

namespace LouisAgent.Core.Hosting.Messages;
// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Exception texts for the host and session area: what is wrong and how to fix it.</summary>
public enum HostingErrorMessage
{
    [Description("This host only exposes tools; it has no language model.")]
    ToolHostHasNoModel,

    [Description("No '{Tool}' tool in the toolsets {Toolsets}.")]
    ToolNotInToolsets,

    [Description(
        "MINION_LLMS names '{Name}' for '{Role}', which is neither a profile in LLM_SESSION_PROFILES ({Available}) nor 'instance'.")]
    MinionLlmUnknown,

    [Description("Unknown LLM profile '{Name}'. Available: {Available}.")]
    UnknownSessionLlm,

    [Description("LLM_SESSION_PROFILES names '{Name}', but {File} isn't there.")]
    SessionLlmFileMissing,

    [Description("'{Name}' isn't an LLM profile name: use lowercase letters, digits and dashes.")]
    SessionLlmNameInvalid,
}
