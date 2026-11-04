using LouisAgent.Choices;
using LouisAgent.Composition.Packs;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Packs.Paymo;

/// <summary>Paymo time tracking as tools: time entries, tasks, projects and summaries.</summary>
public sealed class PaymoPack(PaymoOptions options) : IAgentPack
{
    /// <inheritdoc/>
    public string Name => PaymoToolset.Paymo.Wire();

    /// <inheritdoc/>
    public IReadOnlyList<PackToolset> Toolsets { get; } =
    [
        new(PaymoToolset.Paymo.Wire(), "Paymo time tracking: time entries, tasks, projects and summaries",
            GuideFile: PackToolset.Guide(PaymoToolset.Paymo.Wire()),
            MissingSettings: _ => string.IsNullOrWhiteSpace(options.ApiKey) ? ["PAYMO_API_KEY"] : [],
            ToolTypes: [typeof(PaymoTools)],
            CreateTools: _ => [new PaymoTools(options)]),
    ];

    /// <inheritdoc/>
    public IFileProvider Prompts { get; } = new ManifestEmbeddedFileProvider(typeof(PaymoPack).Assembly, "Prompts");

    /// <inheritdoc/>
    public IReadOnlyList<string> Requires => [];
}

/// <summary>The name this pack's toolset goes by (ADR 0043).</summary>
public enum PaymoToolset
{
    [WireName("paymo")] Paymo,
}
