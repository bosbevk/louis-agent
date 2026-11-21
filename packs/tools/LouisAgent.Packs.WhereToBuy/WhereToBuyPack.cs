using LouisAgent.Composition.Packs;
using LouisAgent.Choices;
using LouisAgent.Packs.WhereToBuy.Branches;
using LouisAgent.Packs.WhereToBuy.Tools;
using Microsoft.Extensions.FileProviders;
using LouisAgent.Packs.WhereToBuy.Branches.Map;

namespace LouisAgent.Packs.WhereToBuy;

/// <summary>
/// Where to buy as a pack: one toolset, <c>where-to-buy</c>, that finds a shop's branches near the user from
/// OpenStreetMap. It needs no setting and no browser, so it loads wherever it is named. It also ships the shop checks
/// an eval names, which prove the answer names the branch and reads no shop.
/// </summary>
/// <remarks>
/// The same shape as every pack (ADR 0027): a list of toolsets and the files the model reads, naming no identity and
/// no other pack. What things cost is the <c>shop-prices</c> pack's job (Louis, 2026-11-04: one job each). It took
/// the place of the shops pack, whose Checkers catalogue was dropped once no shop gave permission (ADR 0069).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0027-engine-identities-and-packs.md">ADR 0027: engine, identities and packs</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0028-packs-stay-convertible-to-mcp.md">ADR 0028: packs stay convertible to MCP</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/packs/docs/adr/0069-shop-prices-are-stats-sa-estimates.md">ADR 0069: shop prices are Stats SA's averages</seealso>
public sealed class WhereToBuyPack : IAgentPack
{
    /// <summary>Builds the pack over the map the process shares.</summary>
    public WhereToBuyPack()
    {
        Toolsets =
        [
            new(WhereToBuyToolset.WhereToBuy.Wire(),
                "A shop's branches or the supermarkets near the user's town, with its municipality and province " +
                "(OpenStreetMap)",
                GuideFile: PackToolset.Guide(WhereToBuyToolset.WhereToBuy.Wire()),
                MissingSettings: _ => [],
                ToolTypes: [typeof(WhereToBuyTools)],
                CreateTools: _ => [new WhereToBuyTools(new BranchFinder(MapSearch.Default))],
                // Lookups change nothing, and the map's pace is kept inside MapSearch.
                ParallelSafe: true),
        ];
    }

    /// <inheritdoc />
    public string Name => WhereToBuyToolset.WhereToBuy.Wire();

    /// <inheritdoc />
    public IReadOnlyList<PackToolset> Toolsets { get; }

    /// <inheritdoc />
    public IFileProvider Prompts { get; } =
        EmbeddedPrompts.Of(typeof(WhereToBuyPack).Assembly);

    /// <inheritdoc />
    public IReadOnlyList<string> Requires => [];
}
