using LouisAgent.Choices;
using LouisAgent.Composition.Packs;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Packs.DevOps;

/// <summary>Azure DevOps as tools: work items, sprints and team queries.</summary>
public sealed class DevOpsPack(DevOpsOptions options) : IAgentPack
{
    /// <inheritdoc/>
    public string Name => DevOpsToolset.DevOps.Wire();

    /// <inheritdoc/>
    public IReadOnlyList<PackToolset> Toolsets { get; } =
    [
        new(DevOpsToolset.DevOps.Wire(), "Azure DevOps work items, sprints and team queries",
            GuideFile: PackToolset.Guide(DevOpsToolset.DevOps.Wire()),
            MissingSettings: _ => options.Missing(),
            ToolTypes: [typeof(DevOpsTools)],
            CreateTools: _ => [new DevOpsTools(options.ApiKey!, options.Organization!, options.Project!, options.Team)]),
    ];

    /// <inheritdoc/>
    public IFileProvider Prompts { get; } = new ManifestEmbeddedFileProvider(typeof(DevOpsPack).Assembly, "Prompts");

    /// <inheritdoc/>
    public IReadOnlyList<string> Requires => [];
}

/// <summary>The name this pack's toolset goes by (ADR 0043).</summary>
public enum DevOpsToolset
{
    [WireName("devops")] DevOps,
}
