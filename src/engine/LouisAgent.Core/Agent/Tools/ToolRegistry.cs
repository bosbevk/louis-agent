using LouisAgent.Composition.Logging;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Agent.Catalog;
using System.Reflection;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.AI;
using LouisAgent.Core.Logging;
using Microsoft.Extensions.Logging;
using LouisAgent.Core.Agent.Messages;
using LouisAgent.Core.Agent.Messages.Holes.Discovery;
using LouisAgent.Core.Agent.Messages.Holes.Toolsets;
using LouisAgent.Core.Agent.Replay;
using LouisAgent.Core.Sessions.Replay.Store;

namespace LouisAgent.Core.Agent.Tools;

/// <summary>
/// The agent's tools, kept in name order (ordinal), so the tool list, and with it the start of every request, is the
/// same bytes from one run to the next (F4: the prompt cache is keyed by the exact prefix, and reflection's method
/// order isn't guaranteed). A tool added later (an agent-built one) still changes the list, as any change must. Tool
/// names are unique across all toolsets: a second tool with a name already in the list throws. When the model takes no
/// tools (<paramref name="supportsTools"/> false), it won't register tools discovered elsewhere either. When the engine
/// keeps recordings (<paramref name="replay"/>), every tool is recorded around as it comes in (FX235), so a free replay
/// plays back what each tool answered.
/// </summary>
internal sealed class ToolRegistry(bool supportsTools = true, ReplayStore? replay = null)
{
    private static readonly ILogger Log = AgentLogging.For<ToolRegistry>();

    private static readonly Comparer<AITool> ByName =
        Comparer<AITool>.Create((a, b) => string.CompareOrdinal(a.Name, b.Name));

    /// <summary>
    /// The live list the engine sends with every request (<c>BeginTurn</c>, the loop's final request). Every open chat
    /// shares it, so a change reaches the next request of each; FX4 gives each conversation its own copy.
    /// </summary>
    internal List<AITool> Live { get; } = [];

    /// <summary>How many tools there are.</summary>
    public int Count => Live.Count;

    /// <summary>
    /// Adds a tool at its place in name order, recorded around when the engine keeps recordings; a name already in the
    /// list throws.
    /// </summary>
    public void Add(AITool tool)
    {
        int at = Live.BinarySearch(tool, ByName);
        if (at >= 0)
        {
            var twice = new DuplicateToolNameHoles(tool.Name);
            throw new InvalidOperationException(MessageText.Format(AgentErrorMessage.DuplicateToolName, twice));
        }

        Live.Insert(~at, RecordedTool.Around(tool, replay));
    }

    /// <summary>
    /// Adds every public instance method the object declares as a tool, named and described by the method, by the one
    /// rule the catalogue states (<see cref="AgentCatalog.ToolMethodsOfType"/>, <see cref="AgentCatalog.FunctionOf"/>),
    /// so what the engine registers is what the convertibility test checks (CL15).
    /// </summary>
    public void AddPublicMethods(object target)
    {
        foreach (MethodInfo method in AgentCatalog.ToolMethodsOfType(target.GetType()))
            Add(AgentCatalog.FunctionOf(method, _ => target));
    }

    /// <summary>
    /// Takes a tool out of the list, the one given or the recorded tool around it; false when it wasn't there.
    /// </summary>
    public bool Remove(AITool tool) =>
        Live.RemoveAll(live => ReferenceEquals(live, tool) ||
                               live is RecordedTool recorded && recorded.Wraps(tool)) > 0;

    /// <summary>
    /// Keeps the one tool named and drops every other, the clock included (F30: a lookup agent
    /// gets one task and one tool, so its prompt lists exactly what it may call). Before the engine's first turn.
    /// </summary>
    public void KeepOnly(string name)
    {
        foreach (var other in Live.Where(t => !string.Equals(t.Name, name, StringComparison.Ordinal)).ToList())
            Remove(other);
    }

    /// <summary>
    /// Whether a tool has this name, case ignored.
    /// </summary>
    public bool Contains(string name, StringComparison comparison = StringComparison.Ordinal) =>
        Live.Any(t => t.Name.Equals(name, comparison));

    /// <summary>
    /// Notes a tool found outside the engine, such as one of Rider's. It only logs it for now, since running one needs
    /// a whole MCP client; false when its name is taken or the model takes no tools.
    /// </summary>
    public bool RegisterDiscovered(string name, string description)
    {
        if (!supportsTools)
        {
            var refused = new McpCannotRegisterHoles(name);
            Log.Log(LogLevel.Warning, AgentLogMessage.McpCannotRegister, refused, tag: "MCP");
            return false;
        }

        if (Contains(name))
        {
            var taken = new McpToolAlreadyRegisteredHoles(name);
            Log.Log(LogLevel.Information, AgentLogMessage.McpToolAlreadyRegistered, taken, tag: "MCP");
            return false;
        }

        var found = new McpToolDiscoveredHoles(name, description);
        Log.Log(LogLevel.Information, AgentLogMessage.McpToolDiscovered, found, tag: "MCP");
        return true;
    }
}
