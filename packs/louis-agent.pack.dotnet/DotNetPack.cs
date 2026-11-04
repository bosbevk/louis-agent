using LouisAgent.Choices;
using LouisAgent.Composition.Packs;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Packs.DotNet;

/// <summary>The .NET SDK as tools, for an agent that works on .NET code.</summary>
public sealed class DotNetPack : IAgentPack
{
    /// <inheritdoc/>
    public string Name => DotNetToolset.DotNet.Wire();

    /// <inheritdoc/>
    public IReadOnlyList<PackToolset> Toolsets { get; } =
    [
        new(DotNetToolset.DotNet.Wire(), "Build, test, run and restore .NET projects; list packages; SDK information",
            GuideFile: PackToolset.Guide(DotNetToolset.DotNet.Wire()),
            MissingSettings: _ => [],
            ToolTypes: [typeof(DotNetTools)],
            CreateTools: context => [new DotNetTools(Path.GetFullPath(context.WorkspaceRoot))]),
    ];

    /// <inheritdoc/>
    public IFileProvider Prompts { get; } = new ManifestEmbeddedFileProvider(typeof(DotNetPack).Assembly, "Prompts");

    /// <inheritdoc/>
    public IReadOnlyList<string> Requires => [];
}

/// <summary>The name this pack's toolset goes by (ADR 0043).</summary>
public enum DotNetToolset
{
    DotNet,
}
