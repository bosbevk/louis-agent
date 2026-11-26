using System.ComponentModel;

namespace LouisAgent.Mcp.Messages;

/// <summary>
/// What the MCP server answers its client of its own: a missing API key.
/// </summary>
public enum McpText
{
    [Description("Missing or invalid API key (x-api-key or Authorization: Bearer).")]
    InvalidApiKey,
}
