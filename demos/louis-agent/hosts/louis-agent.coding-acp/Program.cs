using LouisAgent.AcpServer;
using LouisAgent.Core.Agent.Catalog;

// The coding agent inside Rider. It names no packs, so it works with the engine's own tools.
await AcpHost.RunAsync(AgentCatalog.CoreOnly);
