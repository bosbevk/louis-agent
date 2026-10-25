namespace LouisAgent.Core.Messages;

using System.ComponentModel;

// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>The message system's own texts, and the one text two hosts share (ApiKeyRequired, the API's and the MCP server's).</summary>
public enum ErrorMessage
{
    [Description("{Message} needs a value for '{Hole}'.")]
    MessageNeedsValue,

    [Description("{Message} has no [Description].")]
    MessageHasNoTemplate,

    [Description("{Setting} is required: set it in config/.env.secrets, or set AGENT_AUTH=off to serve without a key on purpose.")]
    ApiKeyRequired,
}
