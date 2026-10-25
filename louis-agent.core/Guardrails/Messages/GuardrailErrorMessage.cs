namespace LouisAgent.Core.Guardrails.Messages;

using System.ComponentModel;

// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Exception texts for the guardrails area: what is wrong and how to fix it.</summary>
public enum GuardrailErrorMessage
{
    [Description("Unknown AGENT_AUTH '{Value}'. Use {Supported}.")]
    UnknownAuthMode,

    [Description("Workspace root cannot be empty.")]
    WorkspaceRootEmpty,
}
