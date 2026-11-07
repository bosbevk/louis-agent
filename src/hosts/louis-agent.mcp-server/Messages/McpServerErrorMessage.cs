using System.ComponentModel;

namespace LouisAgent.McpServer.Messages;

/// <summary>Exception texts for the MCP server: what is wrong with a setting and how to fix it.</summary>
public enum McpServerErrorMessage
{
    [Description("Unknown MCP_TRANSPORT '{Value}'. Use {Supported}.")]
    UnknownMcpTransport,
}
