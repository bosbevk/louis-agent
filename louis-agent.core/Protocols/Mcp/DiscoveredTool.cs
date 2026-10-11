namespace LouisAgent.Core.Protocols.Mcp;

using System.Text.Json;

/// <summary>Tool discovered from an MCP server.</summary>
public sealed record DiscoveredTool(
    string Name,
    string Description,
    JsonElement InputSchema);
