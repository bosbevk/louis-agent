namespace louis_agent.core.mcp;

using louis_agent.core.tools;

/// <summary>
/// Orchestrates tool discovery from Rider's MCP server and registers them with AgentEngine.
/// Discovered tools are prefixed with "rider_" to avoid conflicts with native tools.
/// Requires IJ_MCP_SERVER_PROJECT_PATH header (sent automatically from WORKSPACE_ROOT).
/// </summary>
public sealed class RiderMcpToolDiscovery
{
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
                Console.Error.WriteLine("[MCP] No tools discovered from Rider MCP server");
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
                    Console.Error.WriteLine($"[MCP] Failed to register tool '{tool.Name}': {ex.Message}");
                }
            }

            Console.Error.WriteLine($"[MCP] Discovered and registered {registeredCount}/{tools.Count} tools from Rider");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[MCP] Tool discovery failed: {ex.Message}");
            if (throwOnError)
                throw;
        }
    }
}
