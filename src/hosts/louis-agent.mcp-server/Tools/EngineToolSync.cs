using LouisAgent.Composition.Logging;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Logging;
using LouisAgent.McpServer.Messages;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LouisAgent.McpServer.Tools;

/// <summary>
/// Mirrors <see cref="AgentEngine.Tools"/> into the MCP tool collection. Changing the collection makes the server
/// send notifications/tools/list_changed, so clients pick up agent-built tools without reconnecting.
/// </summary>
public static class EngineToolSync
{
    /// <summary>
    /// Publishes the engine's tools now, and again every time the engine's tool list changes.
    /// </summary>
    public static void Attach(AgentEngine engine, McpServerPrimitiveCollection<McpServerTool> collection)
    {
        // Name -> (engine function, its published MCP tool, description at publish time).
        var published =
            new Dictionary<string, (AIFunction Function, McpServerTool Tool, string Description)>(
                StringComparer.Ordinal);
        object gate = new();

        /// <summary>
        /// Takes down the tools that are gone or changed, and puts up the ones the client doesn't have yet.
        /// </summary>
        void Sync()
        {
            lock (gate)
            {
                // One list_changed notification per sync, not one per added/removed tool.
                using var _ = collection.DeferChangedEvents();
                var current = engine.Tools.OfType<AIFunction>().ToDictionary(f => f.Name, StringComparer.Ordinal);

                foreach (var (name, entry) in published.ToList())
                {
                    // Replaced (overwrite) or re-described (approval removes "pending approval") tools are republished.
                    if (current.TryGetValue(name, out var function) && ReferenceEquals(function, entry.Function) &&
                        function.Description == entry.Description)
                        continue;

                    collection.Remove(entry.Tool);
                    published.Remove(name);
                }

                foreach (var (name, function) in current)
                {
                    if (published.ContainsKey(name)) continue;

                    var tool = McpServerTool.Create(function);
                    if (collection.TryAdd(tool)) published[name] = (function, tool, function.Description);
                    else
                        AgentLogging.LoggerFactory.CreateLogger("LouisAgent.McpServer").Log(LogLevel.Information,
                            McpServerLogMessage.ToolNameTaken, new { Name = name }, tag: "MCP");
                }
            }
        }

        Sync();
        engine.ToolsChanged += Sync;
    }
}
