using LouisAgent.Cli;
using LouisAgent.Composition;
using LouisAgent.Core.Hosting;

// Usage: louis-agent.cli [prompt]. Provider/model come from LLM_PROVIDER / LLM_MODEL (see config/.env.example).
var host = AgentHost.Build(Plugins.FromEnvironment());

if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
{
    await new ConsoleChat(host.Engine).RunSinglePromptAsync(args[0]);
}
else
{
    await new ConsoleChat(host.Engine).RunAsync();
}
