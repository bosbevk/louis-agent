using LouisAgent.AcpServer;
using LouisAgent.Core.Hosting;

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
