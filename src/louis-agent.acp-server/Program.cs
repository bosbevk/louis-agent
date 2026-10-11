using louis_agent.acp_server;
using louis_agent.core;

try
{
    var host = AgentHost.Build();
    await new AcpServer(host.Engine, usageInChat: host.Agent.UsageInChat).RunAsync();
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    Environment.ExitCode = 1;
}
