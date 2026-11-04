using System.ComponentModel;
using LouisAgent.Choices;
using LouisAgent.Core.Agent.Catalog;

namespace LouisAgent.Testing;

/// <summary>
/// The toolsets an engine test needs by name now that core has no tools of its own. The names are the real ones, so a
/// test reads the way a session does, but nothing here is a real pack.
/// </summary>
public enum TestToolset
{
    DotNet,
}

/// <summary>
/// A pack of small fake tools under the real toolset names, so the engine's tests prove the engine without a real pack.
/// </summary>
/// <remarks>
/// Test double, because the engine moves to its own package and its tests go with it: a real pack here would tie the
/// loop to what it loads. Register <see cref="Pack"/>'s prompts before the first prompt is read, as a host does.
/// </remarks>
public static class TestTools
{
    /// <summary>The fake pack.</summary>
    public static FakePack Pack { get; } = new("test-tools",
        [
            FakePack.Toolset(TestToolset.DotNet.Wire(), typeof(FakeDotNetTools), _ => [new FakeDotNetTools()]),
        ],
        new Dictionary<string, string>
        {
            [$"toolsets/{TestToolset.DotNet.Wire()}.md"] = "# .NET Skills\n\nBuilds things.",
        });

    /// <summary>A catalogue holding it, and what core has.</summary>
    public static AgentCatalog Catalog { get; } = new([], [Pack]);
}

/// <summary>The fake dotnet toolset.</summary>
public sealed class FakeDotNetTools
{
    /// <summary>Pretends to build.</summary>
    [Description("Builds a project.")]
    public string DotNetBuild() => "built";
}
