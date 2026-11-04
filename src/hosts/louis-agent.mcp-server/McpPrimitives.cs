using LouisAgent.Core.Agent;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.McpServer.Resources;
using LouisAgent.McpServer.Tools;
using ModelContextProtocol.Server;

namespace LouisAgent.McpServer;

/// <summary>
/// What the server publishes, set up once for both transports: the engine's tools, kept in step as the agent builds
/// them, and the catalogue's feeds as resources (F56).
/// </summary>
/// <remarks>
/// Facade (GoF): stdio and HTTP each used to register the tools on their own, so a new primitive had two places to be
/// forgotten in; now each transport makes one call. The MCP-only skill tools stay a <c>WithTools</c> registration in
/// each transport, since the SDK builds that class from its services.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0070-packs-publish-data-feeds.md">ADR 0070: packs publish data feeds</seealso>
public static class McpPrimitives
{
    /// <summary>Puts the engine's tools and the catalogue's feeds into the server's options.</summary>
    public static void Add(McpServerOptions options, AgentEngine engine, AgentCatalog catalog)
    {
        options.ToolCollection ??= [];
        EngineToolSync.Attach(engine, options.ToolCollection);

        options.ResourceCollection ??= [];
        foreach (var resource in FeedResources.From(catalog.Feeds))
            options.ResourceCollection.Add(resource);
    }
}
