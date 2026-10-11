using LouisAgent.Core.Agent;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Agent;

// The tool list on its own: name order, unique names, discovered tools. ToolOrderTests checks it through the engine.
public class ToolRegistryTests
{
    private static AITool Tool(string name) => AIFunctionFactory.Create(() => "ok", name);

    [Test]
    public void Add_KeepsNameOrder_WhateverOrderToolsArrive()
    {
        var registry = new ToolRegistry();
        foreach (string name in new[] { "Write", "Append", "read", "Delete" }) registry.Add(Tool(name));

        Assert.That(registry.Live.Select(t => t.Name), Is.EqualTo(new[] { "Append", "Delete", "Write", "read" }), "ordinal: capitals first");
    }

    [Test]
    public void Add_ASecondToolWithAName_Throws()
    {
        var registry = new ToolRegistry();
        registry.Add(Tool("Ping"));

        Assert.That(() => registry.Add(Tool("Ping")), Throws.InvalidOperationException.With.Message.Contain("Two tools are named 'Ping'"));
    }

    [Test]
    public void AddPublicMethods_AddsDeclaredPublicMethodsOnly()
    {
        var registry = new ToolRegistry();
        registry.AddPublicMethods(new Toolset());

        Assert.That(registry.Live.Select(t => t.Name), Is.EqualTo(new[] { "First", "Second" }));
        Assert.That(registry.Live[0].Description, Is.EqualTo("The first tool"));
    }

    private sealed class Toolset
    {
        [System.ComponentModel.Description("The second tool")] public string Second() => "2";
        [System.ComponentModel.Description("The first tool")] public string First() => "1";
        internal string NotATool() => "no";
        private string AlsoNot() => "no";
    }

    [Test]
    public void Contains_IsOrdinal_UnlessAskedOtherwise()
    {
        var registry = new ToolRegistry();
        registry.Add(Tool("CsvReport"));

        Assert.That(registry.Contains("csvreport"), Is.False);
        Assert.That(registry.Contains("csvreport", StringComparison.OrdinalIgnoreCase), Is.True);
    }

    [Test]
    public void Remove_TakesTheToolOut()
    {
        var registry = new ToolRegistry();
        AITool tool = Tool("Gone");
        registry.Add(tool);

        Assert.That(registry.Remove(tool), Is.True);
        Assert.That(registry.Count, Is.Zero);
    }

    [Test]
    public void RegisterDiscovered_RefusesANameAlreadyUsed()
    {
        var registry = new ToolRegistry();
        registry.Add(Tool("rider_find"));

        Assert.That(registry.RegisterDiscovered("rider_find", "Find in Rider"), Is.False);
        Assert.That(registry.RegisterDiscovered("rider_open", "Open in Rider"), Is.True);
    }
}
