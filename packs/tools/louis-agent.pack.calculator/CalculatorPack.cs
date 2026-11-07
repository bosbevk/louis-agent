using LouisAgent.Composition.Packs;
using LouisAgent.Choices;
using LouisAgent.Packs.Calculator.Tools;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Packs.Calculator;

/// <summary>
/// A calculator as a pack: one toolset, <c>calculator</c>, so a model hands its sums to code instead of doing them in
/// its head. It needs no setting and no network, so it loads wherever it is named.
/// </summary>
/// <remarks>
/// The same shape as every pack (ADR 0027): a list of toolsets and the files the model reads, naming no identity and no
/// other pack. General tools come before the engine's, as the web pack's do, since the order is part of the prefix the
/// model's cache keys on.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0027-engine-identities-and-packs.md">ADR 0027: engine, identities and packs</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0028-packs-stay-convertible-to-mcp.md">ADR 0028: packs stay convertible to MCP</seealso>
public sealed class CalculatorPack : IAgentPack
{
    /// <inheritdoc />
    public string Name => CalculatorToolset.Calculator.Wire();

    /// <inheritdoc />
    public IReadOnlyList<PackToolset> Toolsets { get; } =
    [
        new(CalculatorToolset.Calculator.Wire(),
            "Sums worked out in code, instead of in the model's head",
            GuideFile: PackToolset.Guide(CalculatorToolset.Calculator.Wire()),
            MissingSettings: _ => [],
            ToolTypes: [typeof(CalculatorTools)],
            CreateTools: _ => [new CalculatorTools()],
            // A sum reads nothing and changes nothing.
            ParallelSafe: true),
    ];

    /// <inheritdoc />
    public IFileProvider Prompts { get; } =
        new ManifestEmbeddedFileProvider(typeof(CalculatorPack).Assembly, "Prompts");

    /// <inheritdoc />
    public IReadOnlyList<string> Requires => [];

    /// <inheritdoc />
    public bool ComesBeforeTheEngine => true;
}
