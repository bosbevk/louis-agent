using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using louis_agent.core;
using louis_agent.core.tools;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

// The MCP client brings its own model; this server publishes the agent's tools (workspace, git, DotNet*, Python*,
// PowerShell*, Bash*, skills, agent-built tools, ...) so that client can call them.
var engine = AgentHost.BuildToolHost();

// Default (unset or anything but "http"): stdio, for a client that launches this as a local subprocess (Rider,
// Claude Desktop) - unchanged from before. "http" serves Streamable HTTP instead, for a remote client that isn't
// local (an agent built with another SDK/provider, e.g. OpenAI's Agents SDK, connecting over the network).
bool useHttp = string.Equals(Environment.GetEnvironmentVariable("MCP_TRANSPORT"), "http", StringComparison.OrdinalIgnoreCase);

if (useHttp)
{
    await RunHttpAsync(args, engine);
}
else
{
    await RunStdioAsync(args, engine);
}

static async Task RunStdioAsync(string[] args, AgentEngine engine)
{
    var builder = Host.CreateApplicationBuilder(args);

    // Log to stderr only (critical for stdio transport)
    builder.Logging.AddConsole(opts =>
    {
        opts.LogToStandardErrorThreshold = LogLevel.Trace;
    });

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

static async Task RunHttpAsync(string[] args, AgentEngine engine)
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.AddConsole(opts =>
    {
        opts.LogToStandardErrorThreshold = LogLevel.Trace;
    });

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
    // AGENT_API_KEY when unset, so a single shared key still works for anyone who hasn't split them.
    string? apiKey = Environment.GetEnvironmentVariable("MCP_API_KEY") ?? Environment.GetEnvironmentVariable("AGENT_API_KEY");
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        Console.Error.WriteLine("[WARN] MCP_API_KEY (or AGENT_API_KEY) not set; the MCP HTTP endpoint accepts unauthenticated requests. Only expose it on localhost or behind your own auth.");
    }
    else
    {
        app.Use(async (context, next) =>
        {
            if (HasApiKey(context.Request, apiKey))
            {
                await next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Missing or invalid API key (x-api-key or Authorization: Bearer).");
        });
    }

    app.MapMcp();
    await app.RunAsync();
}

static bool HasApiKey(HttpRequest request, string expected)
{
    string presented = request.Headers["x-api-key"].FirstOrDefault()
                       ?? request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase)
                       ?? string.Empty;
    return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(presented), Encoding.UTF8.GetBytes(expected));
}

/// <summary>
/// Mirrors <see cref="AgentEngine.Tools"/> into the MCP tool collection. Changing the collection makes the server
/// send notifications/tools/list_changed, so clients pick up agent-built tools without reconnecting.
/// </summary>
internal static class EngineToolSync
{
    public static void Attach(AgentEngine engine, McpServerPrimitiveCollection<McpServerTool> collection)
    {
        // Name -> (engine function, its published MCP tool, description at publish time).
        var published = new Dictionary<string, (AIFunction Function, McpServerTool Tool, string Description)>(StringComparer.Ordinal);
        object gate = new();

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
                    else Console.Error.WriteLine($"[MCP] Tool name '{name}' is already published; skipping.");
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

    public SkillsToolProvider(AgentEngine engine)
    {
        _engine = engine;
    }

    [McpServerTool]
    [Description("List all available skills (run one with ExecuteSkill)")]
    public string ListSkills()
    {
        var skillList = string.Join("\n", _engine.Skills.OrderBy(s => s.Name).Select(s => $"- {s.Name} ({s.Language}): {s.Description}"));
        return $"Available skills:\n{skillList}";
    }

    [McpServerTool]
    [Description("Get detailed information about a specific skill, including its command")]
    public string GetSkillInfo(
        [Description("Name of the skill to get information about")] string skillName)
    {
        var skill = _engine.Skills.FirstOrDefault(s =>
            s.Name.Equals(skillName, StringComparison.OrdinalIgnoreCase));

        if (skill == null)
        {
            return $"Unknown skill: '{skillName}'";
        }

        return $"Skill: {skill.Name}\nDescription: {skill.Description}\nLanguage: {skill.Language}\nCommand: {skill.Command}";
    }

    [McpServerTool]
    [Description("Get the agent's full guidance: default instructions plus the .NET, Python, PowerShell, Bash, self-extension and skill documentation")]
    public string GetSkillDocumentation()
    {
        return _engine.SkillDocumentation;
    }
}
