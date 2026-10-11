namespace LouisAgent.McpServer.Messages;

using System.ComponentModel;

/// <summary>The MCP server's log lines; written to stderr, never stdout, which carries the protocol.</summary>
public enum McpServerLogMessage
{
    [Description("MCP_API_KEY (or AGENT_API_KEY) not set; the MCP HTTP endpoint accepts unauthenticated requests. Only expose it on localhost or behind your own auth.")]
    NoApiKey,

    [Description("Tool name '{Name}' is already published; skipping.")]
    ToolNameTaken,
}
