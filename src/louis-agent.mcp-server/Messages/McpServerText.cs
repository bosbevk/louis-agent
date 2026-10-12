namespace LouisAgent.McpServer.Messages;

using System.ComponentModel;

/// <summary>What the MCP server answers its client: an unknown skill, a missing API key.</summary>
public enum McpServerText
{
    [Description("Missing or invalid API key (x-api-key or Authorization: Bearer).")]
    InvalidApiKey,

    [Description("Unknown skill: '{Name}'")]
    UnknownSkill,
}
