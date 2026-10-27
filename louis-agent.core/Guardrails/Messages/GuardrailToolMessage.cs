
using System.ComponentModel;

namespace LouisAgent.Core.Guardrails.Messages;
// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Strings for the guardrails, returned to the model: what went wrong and what to do instead.</summary>
public enum GuardrailToolMessage
{
    [Description("refusing to fetch {Host}: local addresses are not allowed.")]
    FetchLocalAddress,

    [Description("could not resolve {Host}")]
    FetchUnresolvable,

    [Description("refusing to fetch {Host}: it resolves to a private or local address ({Address}).")]
    FetchPrivateAddress,

    [Description("refusing to connect to {Host}: private or local address.")]
    ConnectPrivateAddress,

    [Description("Workspace paths must be relative.")]
    PathNotRelative,

    [Description("Path must stay inside the workspace.")]
    PathOutsideWorkspace,

    [Description("Workspace paths cannot traverse symbolic links.")]
    PathThroughSymbolicLink,
}
