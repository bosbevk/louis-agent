using LouisAgent.Cli;
using LouisAgent.Core.Agent.Catalog;

// Usage: louis-agent.coding-cli [prompt]. The coding agent names no packs, so it works with the engine's own tools.
await CliHost.RunAsync(args, AgentCatalog.CoreOnly);
