using louis_agent.core;
using louis_agent.core.tools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

// The MCP client brings its own model; this server publishes the agent's tools (workspace, git, DotNet*, Python*,
// PowerShell*, Bash*, skills, agent-built tools, ...) so that client can call them.
var engine = AgentHost.BuildToolHost();

// Configure host with MCP server
var builder = Host.CreateApplicationBuilder(args);

// Log to stderr only (critical for stdio transport)
builder.Logging.AddConsole(opts =>
{
    opts.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddSingleton(engine);

// Add MCP server with stdio transport
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
