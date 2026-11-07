using LouisAgent.Acp.Messages;
using LouisAgent.Composition.Logging;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Hosting.Discovery;
using LouisAgent.Core.Logging;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Acp.Rider;

/// <summary>
/// Asks Rider's MCP server for its tools when the host starts and registers each on the engine as <c>rider_{name}</c>,
/// so the agent inside Rider can use the IDE's own tools. It runs in the background and gives up quietly when Rider
/// isn't there. The ACP server is Rider's host, so this lives here and not in core (CL57, CL51).
/// </summary>
/// <remarks>
/// Strategy for core's <see cref="IToolDiscovery"/> seam: the coding-acp program hands it in through the host's
/// services, and every other host starts no discovery. Before, core started it for every host and the CLI and the
/// API tried to reach Rider at start-up.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0092-a-project-holds-what-is-reused-together.md">ADR 0092: a project holds what is reused together</seealso>
public sealed class RiderToolDiscovery : IToolDiscovery
{
    private static readonly ILogger Log = AgentLogging.For<RiderToolDiscovery>();
    private const string Tag = "MCP";

    private readonly RiderOptions _options;

    public RiderToolDiscovery(RiderOptions options) => _options = options;

    /// <summary>The discovery the settings describe.</summary>
    public static RiderToolDiscovery FromEnvironment() => new(RiderOptions.FromEnvironment());

    /// <summary>Starts the discovery in the background, or logs that it's off.</summary>
    public void Start(AgentEngine engine)
    {
        if (!_options.AutoDiscover)
        {
            Log.Log(LogLevel.Information, RiderLogMessage.DiscoveryDisabled);
            return;
        }

        Log.Log(LogLevel.Information, RiderLogMessage.DiscoveryStarting, new { _options.Endpoint });
        _ = Task.Run(async () =>
        {
            try
            {
                await DiscoverAndRegisterAsync(engine);
            }
            catch (Exception ex)
            {
                Log.Log(LogLevel.Warning, RiderLogMessage.StartupFailed, new { Error = ex.ToString() }, tag: Tag);
            }
        });
    }

    /// <summary>
    /// Reads Rider's tools and registers each on the engine, counting what took; a failure is a warning, or thrown
    /// when asked to.
    /// </summary>
    public async Task DiscoverAndRegisterAsync(AgentEngine engine, bool throwOnError = false)
    {
        // Rider is told the project by the path it knows (in Docker the host's), else the workspace root.
        using var client = new RiderMcpClient(_options.Endpoint, _options.ProjectPath ?? engine.WorkspaceRoot);
        try
        {
            var tools = await client.DiscoverToolsAsync();
            if (tools.Count == 0)
            {
                Log.Log(LogLevel.Information, RiderLogMessage.NoToolsDiscovered, tag: Tag);
                return;
            }

            int registered = 0;
            foreach (var tool in tools)
                registered += Register(engine, tool) ? 1 : 0;

            Log.Log(LogLevel.Information, RiderLogMessage.Registered, new
            {
                Registered = registered,
                tools.Count
            },
                tag: Tag);
        }
        catch (Exception ex)
        {
            Log.Log(LogLevel.Warning, RiderLogMessage.DiscoveryFailed, new { Reason = ex.Message }, tag: Tag);
            if (throwOnError) throw;
        }
    }

    /// <summary>Registers one tool under Rider's prefix; a refusal is a warning, never the end of the list.</summary>
    private static bool Register(AgentEngine engine, DiscoveredTool tool)
    {
        try
        {
            return engine.RegisterDiscoveredTool($"rider_{tool.Name}", $"[Rider IDE] {tool.Description}");
        }
        catch (Exception ex)
        {
            Log.Log(LogLevel.Warning, RiderLogMessage.RegisterFailed, new
            {
                tool.Name,
                Reason = ex.Message
            },
                tag: Tag);
            return false;
        }
    }
}
