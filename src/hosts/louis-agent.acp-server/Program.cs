using LouisAgent.AcpServer;
using LouisAgent.Composition;
using LouisAgent.Core.Hosting;
using LouisAgent.AcpServer.Messages;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.Logging;

try
{
    var host = AgentHost.Build(Plugins.FromEnvironment());
    await new AcpServer(host.Engine, usageInChat: host.Agent.UsageInChat).RunAsync();
}
catch (Exception ex)
{
    AgentLogging.LoggerFactory.CreateLogger("LouisAgent.AcpServer").Log(LogLevel.Error, AcpLogMessage.ServerStopped, exception: ex, tag: "ACP");
    Environment.ExitCode = 1;
}
