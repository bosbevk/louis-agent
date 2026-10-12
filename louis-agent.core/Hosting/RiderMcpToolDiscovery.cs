namespace LouisAgent.Core.Hosting;

using LouisAgent.Core.Agent;
using LouisAgent.Core.Protocols.Mcp;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.Logging;

/// <summary>
/// Orchestrates tool discovery from Rider's MCP server and registers them with AgentEngine.
/// Discovered tools are prefixed with "rider_" to avoid conflicts with native tools.
/// Requires IJ_MCP_SERVER_PROJECT_PATH header (sent automatically from WORKSPACE_ROOT).
/// </summary>
public sealed class RiderMcpToolDiscovery
{
    private static readonly ILogger Log = AgentLogging.For<RiderMcpToolDiscovery>();

    private readonly RiderMcpClient _client;
    private readonly AgentEngine _agentEngine;

    public RiderMcpToolDiscovery(AgentEngine agentEngine, string? endpoint = null, string? projectPath = null)
    {
        _agentEngine = agentEngine;
        _client = new RiderMcpClient(endpoint ?? "http://127.0.0.1:64482", projectPath);
    }

    /// <summary>
    /// Discover tools from Rider MCP server and register them with AgentEngine.
    /// Runs asynchronously without blocking startup. If Rider is unavailable, silently continues.
    /// </summary>
    public async Task DiscoverAndRegisterAsync(bool throwOnError = false)
    {
        try
        {
            var tools = await _client.DiscoverToolsAsync();

            if (tools.Count == 0)
            {
                Log.Log(LogLevel.Information, LogMessage.McpNoToolsDiscovered, tag: "MCP");
                return;
            }

            var registeredCount = 0;
            foreach (var tool in tools)
            {
                try
                {
                    var toolName = $"rider_{tool.Name}";
                    var description = $"[Rider IDE] {tool.Description}";

                    // Register with AgentEngine
                    var success = _agentEngine.RegisterDiscoveredTool(toolName, description);
                    if (success)
                        registeredCount++;
                }
                catch (Exception ex)
                {
                    Log.Log(LogLevel.Warning, LogMessage.McpRegisterFailed, new { tool.Name, Reason = ex.Message }, tag: "MCP");
                }
            }

            Log.Log(LogLevel.Information, LogMessage.McpRegistered, new { Registered = registeredCount, tools.Count }, tag: "MCP");
        }
        catch (Exception ex)
        {
            Log.Log(LogLevel.Warning, LogMessage.McpDiscoveryFailed, new { Reason = ex.Message }, tag: "MCP");
            if (throwOnError)
                throw;
        }
    }
}
