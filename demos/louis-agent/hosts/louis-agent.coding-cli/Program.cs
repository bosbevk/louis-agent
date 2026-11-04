using LouisAgent.Cli;
using LouisAgent.Coding;

// Usage: louis-agent.coding-cli [prompt]. It loads what this demo's catalogue names.
await CliHost.RunAsync(args, CodingCatalog.Create());
