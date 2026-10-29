using LouisAgent.Choices;
using LouisAgent.Core.Skills.Languages;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Guardrails.Auth;
using System.ComponentModel;
using LouisAgent.Composition;
using LouisAgent.Core.Agent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using LouisAgent.Core.Hosting;
using LouisAgent.McpServer.Messages;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;

// The MCP client brings its own model; this server publishes the agent's tools (workspace, git, DotNet*, Python*,
// PowerShell*, Bash*, skills, agent-built tools, ...) so that client can call them.
var engine = AgentHost.BuildToolHost(Plugins.FromEnvironment());

// Default (unset or anything but "http"): stdio, for a client that launches this as a local subprocess (Rider,
// Claude Desktop) - unchanged from before. "http" serves Streamable HTTP instead, for a remote client that isn't
// local (an agent built with another SDK/provider, e.g. OpenAI's Agents SDK, connecting over the network).
bool useHttp = string.Equals(Environment.GetEnvironmentVariable("MCP_TRANSPORT"), "http",
    StringComparison.OrdinalIgnoreCase);

if (useHttp)
{
    await RunHttpAsync(args, engine);
}
else
{
    await RunStdioAsync(args, engine);
}

/// <summary>
/// Serves the tools over stdin and stdout, for a client on this machine that starts us as its own child process.
/// </summary>
static async Task RunStdioAsync(string[] args, AgentEngine engine)
{
    var builder = Host.CreateApplicationBuilder(args);

    // Log to stderr only (critical for stdio transport)
    builder.Logging.AddConsole(opts => { opts.LogToStandardErrorThreshold = LogLevel.Trace; });

    builder.Services.AddSingleton(engine);

    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithTools<SkillsToolProvider>();

    // Publish the engine's tools and keep them in sync as the agent builds, approves or rejects tools.
    builder.Services.PostConfigure<McpServerOptions>(options =>
    {
        options.ToolCollection ??= [];
        EngineToolSync.Attach(engine, options.ToolCollection);
    });

    var host = builder.Build();
    await host.RunAsync();
}

/// <summary>
/// Serves the tools over the network, behind an API key, for a client somewhere else.
/// </summary>
static async Task RunHttpAsync(string[] args, AgentEngine engine)
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.AddConsole(opts => { opts.LogToStandardErrorThreshold = LogLevel.Trace; });

    builder.Services.AddSingleton(engine);

    builder.Services
        .AddMcpServer()
        .WithHttpTransport()
        .WithTools<SkillsToolProvider>();

    builder.Services.PostConfigure<McpServerOptions>(options =>
    {
        options.ToolCollection ??= [];
        EngineToolSync.Attach(engine, options.ToolCollection);
    });

    var app = builder.Build();

    // The published tools run shell commands and write files - gate every request since this is now reachable
    // over the network instead of only a local stdio pipe. MCP_API_KEY is separate from AGENT_API_KEY (the
    // louis-agent.api key) so access to one can be rotated/revoked without affecting the other; it falls back to
    // AGENT_API_KEY when unset or blank (HostAuth.Key: a bare MCP_API_KEY= in an env file is no key), so a single
    // shared key still works for anyone who hasn't split them. FX18: with neither the server refuses to start, before
    // it listens, unless AGENT_AUTH=off says it is open on purpose (HostAuth, core's one rule for this and the API).
    // Over stdio no key is needed: a child process on this machine.
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

/// <summary>
/// Mirrors <see cref="AgentEngine.Tools"/> into the MCP tool collection. Changing the collection makes the server
/// send notifications/tools/list_changed, so clients pick up agent-built tools without reconnecting.
/// </summary>
internal static class EngineToolSync
{
    /// <summary>
    /// Publishes the engine's tools now, and again every time the engine's tool list changes.
    /// </summary>
    public static void Attach(AgentEngine engine, McpServerPrimitiveCollection<McpServerTool> collection)
    {
        // Name -> (engine function, its published MCP tool, description at publish time).
        var published =
            new Dictionary<string, (AIFunction Function, McpServerTool Tool, string Description)>(
                StringComparer.Ordinal);
        object gate = new();

        /// <summary>
        /// Takes down the tools that are gone or changed, and puts up the ones the client doesn't have yet.
        /// </summary>
        void Sync()
        {
            lock (gate)
            {
                // One list_changed notification per sync, not one per added/removed tool.
                using var _ = collection.DeferChangedEvents();
                var current = engine.Tools.OfType<AIFunction>().ToDictionary(f => f.Name, StringComparer.Ordinal);

                foreach (var (name, entry) in published.ToList())
                {
                    // Replaced (overwrite) or re-described (approval removes "pending approval") tools are republished.
                    if (current.TryGetValue(name, out var function) && ReferenceEquals(function, entry.Function) &&
                        function.Description == entry.Description)
                        continue;

                    collection.Remove(entry.Tool);
                    published.Remove(name);
                }

                foreach (var (name, function) in current)
                {
                    if (published.ContainsKey(name)) continue;

                    var tool = McpServerTool.Create(function);
                    if (collection.TryAdd(tool)) published[name] = (function, tool, function.Description);
                    else
                        AgentLogging.LoggerFactory.CreateLogger("LouisAgent.McpServer").Log(LogLevel.Information,
                            McpServerLogMessage.ToolNameTaken, new { Name = name }, tag: "MCP");
                }
            }
        }

        Sync();
        engine.ToolsChanged += Sync;
    }
}

/// <summary>
/// Read-only views of the loaded skills. Running a skill is done by the engine's ExecuteSkill tool.
/// </summary>
[McpServerToolType]
public class SkillsToolProvider
{
    private readonly AgentEngine _engine;

    /// <summary>
    /// Reads the skills from the engine the server already built.
    /// </summary>
    public SkillsToolProvider(AgentEngine engine)
    {
        _engine = engine;
    }

    /// <summary>
    /// Lists every skill by name, with its language and what it does.
    /// </summary>
    [McpServerTool]
    [Description("List all available skills (run one with ExecuteSkill)")]
    public string ListSkills()
    {
        // One line per skill in name order, under the header (which keeps its newline even when there are none).
        var lines = _engine.Skills.OrderBy(s => s.Name).Select(s => MessageText.Format(McpServerText.SkillListLine,
            new
            {
                s.Name,
                Language = s.Language.Wire(),
                s.Description
            }));
        return $"{MessageText.Format(McpServerText.SkillListHeader)}\n{string.Join("\n", lines)}";
    }

    /// <summary>
    /// Shows one skill in full, its command included, so the client can see what it would run.
    /// </summary>
    [McpServerTool]
    [Description("Get detailed information about a specific skill, including its command")]
    public string GetSkillInfo(
        [Description("Name of the skill to get information about")]
        string skillName)
    {
        var skill = _engine.Skills.FirstOrDefault(s =>
            s.Name.Equals(skillName, StringComparison.OrdinalIgnoreCase));

        if (skill == null)
        {
            return MessageText.Format(McpServerText.UnknownSkill, new { Name = skillName });
        }

        return MessageText.Format(McpServerText.SkillInfo,
            new
            {
                skill.Name,
                skill.Description,
                Language = skill.Language.Wire(),
                skill.Command
            });
    }

    /// <summary>
    /// Hands over all the guidance the agent itself reads, so the client's model can use the tools the same way.
    /// </summary>
    [McpServerTool]
    [Description("Get the agent's full guidance: default instructions plus the .NET, Python, PowerShell, Bash, "
                 + "self-extension and skill documentation")]
    public string GetSkillDocumentation()
    {
        return _engine.SkillDocumentation;
    }
}
