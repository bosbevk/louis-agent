using LouisAgent.Composition.Logging;
using LouisAgent.AcpServer.Messages;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.Logging;

namespace LouisAgent.AcpServer;

/// <summary>
/// The agent inside Rider's AI chat. You say which packs and identities your app has, and this speaks the protocol over
/// stdin and stdout until Rider closes it.
/// </summary>
/// <remarks>
/// Composition Root: your program's entry point is the one place that wires the app, so this library never picks a pack
/// itself. Without that it would have to reference someone's project, and a package can't.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0074-the-folders-say-what-ships.md">ADR 0074: framework and implementation are split</seealso>
public static class AcpHost
{
    /// <summary>Runs the server. If it fails, the reason goes to the log and the process exits with an error.</summary>
    public static async Task RunAsync(AgentCatalog catalog, HostServices? services = null)
    {
        try
        {
            var host = AgentHost.Build(catalog, services ?? HostServices.None);
            await new AcpServer(host.Engine, usageInChat: host.Agent.UsageInChat).RunAsync();
        }
        catch (Exception ex)
        {
            // Stdout is the protocol, so the only place a reason can go is the log on stderr.
            AgentLogging.LoggerFactory.CreateLogger("LouisAgent.AcpServer")
                .Log(LogLevel.Error, AcpLogMessage.ServerStopped, exception: ex, tag: "ACP");
            Environment.ExitCode = 1;
        }
    }
}
