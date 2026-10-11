namespace LouisAgent.Core.Agent;

using System.ComponentModel;
using System.Reflection;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.AI;

/// <summary>
/// The agent's tools, kept in name order (ordinal), so the tool list, and with it the start of every request, is the
/// same bytes from one run to the next (F4: the prompt cache is keyed by the exact prefix, and reflection's method order
/// isn't guaranteed). A tool added later (an agent-built one) still changes the list, as any change must. Tool names are
/// unique across all toolsets: a second tool with a name already in the list throws.
/// </summary>
internal sealed class ToolRegistry
{
    private static readonly Comparer<AITool> ByName = Comparer<AITool>.Create((a, b) => string.CompareOrdinal(a.Name, b.Name));

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
            throw new InvalidOperationException(MessageText.Format(ErrorMessage.DuplicateToolName, new { tool.Name }));
        Live.Insert(~at, tool);
    }

    /// <summary>
    /// Adds every public instance method the object declares as a tool, named and described by the method; the
    /// description with LF line endings, whatever the checkout (<see cref="AgentEngine.Normalise"/>).
    /// </summary>
    public void AddPublicMethods(object target)
    {
        foreach (MethodInfo method in target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            Add(AIFunctionFactory.Create(method, target, new AIFunctionFactoryOptions
            {
                Description = method.GetCustomAttribute<DescriptionAttribute>() is { } description
                    ? AgentEngine.Normalise(description.Description)
                    : null,
            }));
        }
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
            Console.Error.WriteLine($"[MCP] Tool '{name}' already registered; skipping");
            return false;
        }

        // Log discovered tool for informational purposes
        // Full MCP tool invocation would require a complete MCP protocol client
        Console.Error.WriteLine($"[MCP] Discovered tool from Rider: {name} - {description}");
        return true;
    }
}