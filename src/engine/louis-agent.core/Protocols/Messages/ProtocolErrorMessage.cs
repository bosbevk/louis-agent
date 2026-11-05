using LouisAgent.Composition.Messages;
using System.ComponentModel;

namespace LouisAgent.Core.Protocols.Messages;
// The message enums (src/docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger).

/// <summary>Exception texts for the protocols area: what is wrong and how to fix it.</summary>
public enum ProtocolErrorMessage
{
    [Description("Unknown MCP_TRANSPORT '{Value}'. Use {Supported}.")]
    UnknownMcpTransport,
}
