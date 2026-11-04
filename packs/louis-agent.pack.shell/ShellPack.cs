using LouisAgent.Choices;
using LouisAgent.Composition.Packs;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Packs.Shell;

/// <summary>Python, PowerShell and bash as tools: one toolset each, so a session loads only the one it needs.</summary>
public sealed class ShellPack : IAgentPack
{
    /// <inheritdoc/>
    public string Name => ShellPackName.Shell.Wire();

    /// <inheritdoc/>
    public IReadOnlyList<PackToolset> Toolsets { get; } =
    [
        new(ShellToolset.Python.Wire(),
            "Run Python code or scripts and manage packages in the agent's own virtual environment",
            GuideFile: PackToolset.Guide(ShellToolset.Python.Wire()),
            MissingSettings: _ => [],
            ToolTypes: [typeof(PythonTools)],
            CreateTools: context => [new PythonTools(Path.GetFullPath(context.WorkspaceRoot))]),

        new(ShellToolset.PowerShell.Wire(),
            "Run PowerShell scripts (PowerShell 7, or Windows PowerShell 5.1 on Windows)",
            GuideFile: PackToolset.Guide(ShellToolset.PowerShell.Wire()),
            MissingSettings: _ => [],
            ToolTypes: [typeof(PowerShellTools)],
            CreateTools: context => [new PowerShellTools(Path.GetFullPath(context.WorkspaceRoot))]),

        new(ShellToolset.Bash.Wire(), "Run bash scripts (Git Bash on Windows, /bin/bash elsewhere)",
            GuideFile: PackToolset.Guide(ShellToolset.Bash.Wire()),
            MissingSettings: _ => [],
            ToolTypes: [typeof(BashTools)],
            CreateTools: context => [new BashTools(Path.GetFullPath(context.WorkspaceRoot))]),
    ];

    /// <inheritdoc/>
    public IFileProvider Prompts { get; } = new ManifestEmbeddedFileProvider(typeof(ShellPack).Assembly, "Prompts");

    /// <inheritdoc/>
    public IReadOnlyList<string> Requires => [];
}

/// <summary>The names this pack's toolsets go by (ADR 0043).</summary>
public enum ShellToolset
{
    Python,
    PowerShell,
    Bash,
}

/// <summary>The pack's own name, which no toolset shares (ADR 0043).</summary>
public enum ShellPackName
{
    Shell,
}
