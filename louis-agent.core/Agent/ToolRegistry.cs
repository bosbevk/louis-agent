
using LouisAgent.Core.Agent.Catalog;
using System.Reflection;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.AI;
using LouisAgent.Core.Logging;
using Microsoft.Extensions.Logging;
using LouisAgent.Core.Agent.Messages;

namespace LouisAgent.Core.Agent;
/// <summary>
/// The agent's tools, kept in name order (ordinal), so the tool list, and with it the start of every request, is the
/// same bytes from one run to the next (F4: the prompt cache is keyed by the exact prefix, and reflection's method order
/// isn't guaranteed). A tool added later (an agent-built one) still changes the list, as any change must. Tool names are
/// unique across all toolsets: a second tool with a name already in the list throws.
/// </summary>
internal sealed class ToolRegistry
{
    private static readonly ILogger Log = AgentLogging.For<ToolRegistry>();

    private static readonly Comparer<AITool> ByName =
        Comparer<AITool>.Create((a, b) => string.CompareOrdinal(a.Name, b.Name));

    /// <summary>
    /// The live list the engine sends with every request (<c>BeginTurn</c>, the loop's final request). Every open chat
    /// shares it, so a change reaches the next request of each; FX4 gives each conversation its own copy.
    /// </summary>
    internal List<AITool> Live { get; } = [];

    public int Count => Live.Count;

    /// <summary>Adds a tool at its place in name order; a name already in the list throws.</summary>
    public void Add(AITool tool)
    {
        int at = Live.BinarySearch(tool, ByName);
        if (at >= 0)
            throw new InvalidOperationException(MessageText.Format(AgentErrorMessage.DuplicateToolName,
                new { tool.Name }));
        Live.Insert(~at, tool);
    }

    /// <summary>
    /// Adds every public instance method the object declares as a tool, named and described by the method, by the one
    /// rule the catalogue states (<see cref="AgentCatalog.ToolMethodsOfType"/>, <see cref="AgentCatalog.FunctionOf"/>), so
    /// what the engine registers is what the convertibility test checks (CL15).
    /// </summary>
    public void AddPublicMethods(object target)
    {
        foreach (MethodInfo method in AgentCatalog.ToolMethodsOfType(target.GetType()))
            Add(AgentCatalog.FunctionOf(method, _ => target));
    }

    public bool Remove(AITool tool) => Live.Remove(tool);

    /// <summary>Whether a tool has this name; agent-built tools compare without case, as their files are named.</summary>
    public bool Contains(string name, StringComparison comparison = StringComparison.Ordinal) =>
        Live.Any(t => t.Name.Equals(name, comparison));

    /// <summary>
    /// Register a tool discovered from an external source (e.g., Rider MCP).
    /// Currently logs the discovery; full invocation requires MCP protocol implementation.
    /// Returns true if not already registered, false if duplicate.
    /// </summary>
    public bool RegisterDiscovered(string name, string description)
    {
        if (Contains(name))
        {
            Log.Log(LogLevel.Information, AgentLogMessage.McpToolAlreadyRegistered, new { Name = name }, tag: "MCP");
            return false;
        }

        // Log discovered tool for informational purposes
        // Full MCP tool invocation would require a complete MCP protocol client
        Log.Log(LogLevel.Information, AgentLogMessage.McpToolDiscovered, new { Name = name, Description = description },
            tag: "MCP");
        return true;
    }
}
