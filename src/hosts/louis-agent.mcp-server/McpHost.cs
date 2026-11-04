using LouisAgent.Core.Agent;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Guardrails.Auth;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Protocols.Mcp;
using LouisAgent.McpServer.Messages;
using LouisAgent.McpServer.Tools;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LouisAgent.McpServer;

/// <summary>
/// Hands an MCP client the agent's tools. The client brings its own model; this publishes the tools (workspace, git,
/// DotNet*, Python*, PowerShell*, Bash*, skills, agent-built tools, ...) so it can call them, and the packs' feeds as
/// resources it can read (F56).
/// </summary>
/// <remarks>
/// Composition Root: your program's entry point is the one place that wires the app, so this library never picks a pack
/// itself. Without that it would have to reference someone's project, and a package can't.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0074-the-folders-say-what-ships.md">ADR 0074: framework and implementation are split</seealso>
public static class McpHost
{
    /// <summary>Serves the tools from your catalogue, over whichever transport the settings name.</summary>
    public static async Task RunAsync(string[] args, AgentCatalog catalog)
    {
        var engine = AgentHost.BuildToolHost(catalog);

        // Unset is stdio, for a client that launches this as a local subprocess (Rider, Claude Desktop). Http serves
        // Streamable HTTP instead, for a remote client (an agent built with another SDK, connecting over the network).
        // Any other value stops start-up with the names it takes.
        var transport = McpTransportSetting.Parse(Environment.GetEnvironmentVariable("MCP_TRANSPORT"));

        if (transport == McpTransport.Http)
        {
            await RunHttpAsync(args, engine, catalog);
        }
        else
        {
            await RunStdioAsync(args, engine, catalog);
        }
    }

    /// <summary>
    /// Serves the tools over stdin and stdout, for a client on this machine that starts us as its own child process.
    /// </summary>
    private static async Task RunStdioAsync(string[] args, AgentEngine engine, AgentCatalog catalog)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Log to stderr only (critical for stdio transport)
        builder.Logging.AddConsole(opts => { opts.LogToStandardErrorThreshold = LogLevel.Trace; });

        builder.Services.AddSingleton(engine);

        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<SkillsToolProvider>();

        // Publish the engine's tools (kept in sync as the agent builds, approves or rejects tools) and the packs'
        // feeds.
        builder.Services.PostConfigure<McpServerOptions>(options => McpPrimitives.Add(options, engine, catalog));

        var host = builder.Build();
        await host.RunAsync();
    }

    /// <summary>
    /// Serves the tools over the network, behind an API key, for a client somewhere else.
    /// </summary>
    private static async Task RunHttpAsync(string[] args, AgentEngine engine, AgentCatalog catalog)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Logging.AddConsole(opts => { opts.LogToStandardErrorThreshold = LogLevel.Trace; });

        builder.Services.AddSingleton(engine);

        builder.Services
            .AddMcpServer()
            .WithHttpTransport()
            .WithTools<SkillsToolProvider>();

        builder.Services.PostConfigure<McpServerOptions>(options => McpPrimitives.Add(options, engine, catalog));

        var app = builder.Build();

        // The published tools run shell commands and write files - gate every request since this is now reachable
        // over the network instead of only a local stdio pipe. MCP_API_KEY is separate from AGENT_API_KEY (the
        // louis-agent.api key) so access to one can be rotated/revoked without affecting the other; it falls back to
        // AGENT_API_KEY when unset or blank (HostAuth.Key: a bare MCP_API_KEY= in an env file is no key), so a single
        // shared key still works for anyone who hasn't split them. FX18: with neither the server refuses to start,
        // before it listens, unless AGENT_AUTH=off says it is open on purpose (HostAuth, core's one rule for this and
        // the API). Over stdio no key is needed: a child process on this machine.
        string? apiKey = HostAuth.Key(Environment.GetEnvironmentVariable("MCP_API_KEY"),
            Environment.GetEnvironmentVariable("AGENT_API_KEY"));
        switch (HostAuth.AtStartup(HostAuth.Mode(Environment.GetEnvironmentVariable("AGENT_AUTH")), apiKey))
        {
            // Anything this switch doesn't know refuses to start too: the server fails closed, never open by accident.
            case HostAuthDecision.Refuse:
            default:
                throw new InvalidOperationException(MessageText.Format(ErrorMessage.ApiKeyRequired,
                    new { Setting = "MCP_API_KEY (or AGENT_API_KEY)" }));
            case HostAuthDecision.StartOpen:
                AgentLogging.LoggerFactory.CreateLogger("LouisAgent.McpServer")
                    .Log(LogLevel.Warning, McpServerLogMessage.NoApiKey);
                break;
            // Start comes with a key: the decision says so, and the pattern lets the compiler see it.
            case HostAuthDecision.Start when apiKey is { } key:
                app.Use(async (context, next) =>
                {
                    if (HostAuth.HasKey(context.Request.Headers["x-api-key"].FirstOrDefault(),
                            context.Request.Headers.Authorization.FirstOrDefault(), key))
                    {
                        await next(context);
                        return;
                    }

                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync(MessageText.Format(McpServerText.InvalidApiKey));
                });
                break;
        }

        app.MapMcp();
        await app.RunAsync();
    }
}
