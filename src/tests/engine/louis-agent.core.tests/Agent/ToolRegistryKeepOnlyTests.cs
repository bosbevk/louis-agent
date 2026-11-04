using LouisAgent.Core.Agent;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Agent;

/// <summary>Trims a tool list down to the one tool a minion may call (F30).</summary>
public class ToolRegistryKeepOnlyTests
{
    /// <summary>A tool with this name that does nothing.</summary>
    private static AITool Tool(string name) => AIFunctionFactory.Create(() => "ok", name);

    /// <summary>The named tool stays and every other one goes.</summary>
    [Test]
    public void KeepOnly_KeepsTheNamedTool_AndDropsTheRest()
    {
        var registry = new ToolRegistry();
        foreach (string name in new[] { "Append", "Delete", "Write" }) registry.Add(Tool(name));

        registry.KeepOnly("Delete");

        Assert.That(registry.Live.Select(t => t.Name), Is.EqualTo(new[] { "Delete" }));
    }

    /// <summary>The name is matched exactly, so a name that differs by a capital keeps nothing of the other.</summary>
    [Test]
    public void KeepOnly_MatchesTheNameExactly()
    {
        var registry = new ToolRegistry();
        registry.Add(Tool("Delete"));
        registry.Add(Tool("delete"));

        registry.KeepOnly("delete");

        Assert.That(registry.Live.Select(t => t.Name), Is.EqualTo(new[] { "delete" }));
    }
}
