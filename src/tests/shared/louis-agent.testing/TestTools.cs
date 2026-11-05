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
    Git,
    Files,
    Python,
    PowerShell,
    Bash,
    Web,
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
            FakePack.Toolset(TestToolset.Files.Wire(), typeof(FakeFileTools),
                context => [new FakeFileTools(context.WorkspaceRoot)]),
            FakePack.Toolset(TestToolset.Git.Wire(), typeof(FakeGitTools), _ => [new FakeGitTools()]),
            FakePack.Toolset(TestToolset.DotNet.Wire(), typeof(FakeDotNetTools), _ => [new FakeDotNetTools()]),
            FakePack.Toolset(TestToolset.Python.Wire(), typeof(FakePythonTools), _ => [new FakePythonTools()]),
            FakePack.Toolset(TestToolset.PowerShell.Wire(), typeof(FakePowerShellTools), _ => [new FakePowerShellTools()]),
            FakePack.Toolset(TestToolset.Bash.Wire(), typeof(FakeBashTools), _ => [new FakeBashTools()]),
        ],
        new Dictionary<string, string>
        {
            [$"toolsets/{TestToolset.DotNet.Wire()}.md"] = "# .NET Skills\n\nBuilds things.",
            [$"toolsets/{TestToolset.Git.Wire()}.md"] = "# Git Skills\n\nKeeps history.",
            [$"toolsets/{TestToolset.Files.Wire()}.md"] = "# Files Skills\n\nReads and writes.",
            [$"toolsets/{TestToolset.Python.Wire()}.md"] = "# Python Skills\n\nRuns Python.",
            [$"toolsets/{TestToolset.PowerShell.Wire()}.md"] = "# PowerShell Skills\n\nRuns PowerShell.",
            [$"toolsets/{TestToolset.Bash.Wire()}.md"] = "# Bash Skills\n\nRuns bash.",
        }, comesBeforeTheEngine: true);

    /// <summary>A fake pack named as the real web pack is, for a test whose identity names it.</summary>
    public static FakePack WebPack { get; } = new(TestToolset.Web.Wire(),
        [
            FakePack.Toolset(TestToolset.Web.Wire(), typeof(FakeWebTools), _ => [new FakeWebTools()]) with
            {
                ParallelSafe = true,
            },
        ],
        new Dictionary<string, string>
        {
            [$"toolsets/{TestToolset.Web.Wire()}.md"] = "# Web Skills\n\nSearches and reads.",
        }, comesBeforeTheEngine: true);

    /// <summary>A catalogue holding them, and what core has.</summary>
    public static AgentCatalog Catalog { get; } = new([], [Pack, WebPack]);
}

/// <summary>The fake dotnet toolset.</summary>
public sealed class FakeDotNetTools
{
    /// <summary>Pretends to build.</summary>
    [Description("Builds a project.")]
    public string DotNetBuild() => "built";
}

/// <summary>The fake git toolset.</summary>
public sealed class FakeGitTools
{
    /// <summary>Pretends to read the status.</summary>
    [Description("Shows the repository's status.")]
    public string GetStatus() => "clean";
}

/// <summary>The fake files toolset: it really reads and writes, as an engine test may read a file it just made.</summary>
public sealed class FakeFileTools(string workspaceRoot)
{
    /// <summary>Reads the file.</summary>
    [Description("Reads a file.")]
    public string ReadWorkspaceFile([Description("Its path")] string relativePath) =>
        File.ReadAllText(Path.Combine(workspaceRoot, relativePath));

    /// <summary>Writes the file.</summary>
    [Description("Writes a file.")]
    public string WriteWorkspaceFile([Description("Its path")] string relativePath,
        [Description("Its text")] string content)
    {
        File.WriteAllText(Path.Combine(workspaceRoot, relativePath), content);
        return $"wrote {relativePath}";
    }
}

/// <summary>The fake python toolset.</summary>
public sealed class FakePythonTools
{
    /// <summary>Pretends to run.</summary>
    [Description("Runs Python.")]
    public string PythonRun() => "ran";
}

/// <summary>The fake powershell toolset.</summary>
public sealed class FakePowerShellTools
{
    /// <summary>Pretends to run.</summary>
    [Description("Runs PowerShell.")]
    public string PowerShellRun() => "ran";
}

/// <summary>The fake bash toolset.</summary>
public sealed class FakeBashTools
{
    /// <summary>Pretends to run.</summary>
    [Description("Runs bash.")]
    public string BashRun() => "ran";
}

/// <summary>The fake web toolset.</summary>
public sealed class FakeWebTools
{
    /// <summary>Pretends to fetch.</summary>
    [Description("Fetches a page.")]
    public string FetchUrl([Description("Its address")] string url) => $"the page at {url}";

    /// <summary>Pretends to render, as a second tool a minion can be handed.</summary>
    [Description("Reads a page a browser has to draw.")]
    public string ReadRenderedPage([Description("Its address")] string url) => $"the drawn page at {url}";
}
