using LouisAgent.Core.Agent.Catalog;
using LouisAgent.McpServer;

// The coding agent's tools for an MCP client. It names no packs, so it publishes the engine's own tools.
await McpHost.RunAsync(args, AgentCatalog.CoreOnly);
