using LouisAgent.Coding;
using LouisAgent.McpServer;

// The coding agent's tools for an MCP client. It publishes what this demo's catalogue names.
await McpHost.RunAsync(args, CodingCatalog.Create());
