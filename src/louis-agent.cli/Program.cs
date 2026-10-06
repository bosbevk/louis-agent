using louis_agent.core;

// Usage: louis-agent.cli [prompt]. Provider/model come from LLM_PROVIDER / LLM_MODEL (see config/.env.example).
var host = AgentHost.Build();

if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
{
    await host.Engine.RunSinglePromptAsync(args[0]);
}
else
{
    await host.Engine.RunAsync();
}
