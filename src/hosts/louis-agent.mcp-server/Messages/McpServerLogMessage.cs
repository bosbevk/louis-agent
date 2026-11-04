namespace LouisAgent.McpServer.Messages;

using System.ComponentModel;

/// <summary>The MCP server's log lines; written to stderr, never stdout, which carries the protocol.</summary>
public enum McpServerLogMessage
{
    [Description("AGENT_AUTH=off: no MCP_API_KEY (or AGENT_API_KEY), so the MCP HTTP endpoint accepts unauthenticated requests, on purpose. Only expose it on localhost or behind your own auth.")]
    NoApiKey,

    [Description("Tool name '{Name}' is already published; skipping.")]
    ToolNameTaken,
}
