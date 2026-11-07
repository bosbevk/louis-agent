
using System.Text.Json;

namespace LouisAgent.Acp.Rider;
/// <summary>Tool discovered from an MCP server.</summary>
public sealed record DiscoveredTool(
    string Name,
    string Description,
    JsonElement InputSchema);
