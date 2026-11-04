using LouisAgent.Choices;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Skills;
using LouisAgent.Packs.ShopPrices.Prices;
using LouisAgent.Packs.ShopPrices.Tools;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Packs.ShopPrices;

/// <summary>
/// Shop prices as a pack: one toolset, <c>shop-prices</c>, that estimates what a shopping list costs from Stats SA's
/// average prices. It needs no setting and no browser, so it loads wherever it is named.
/// </summary>
/// <remarks>
/// The same shape as every pack (ADR 0027): a list of toolsets and the files the model reads, naming no identity and
/// no other pack. Where to buy is the <c>where-to-buy</c> pack's job, so a specialist can have prices without
/// branches, or branches without prices (Louis, 2026-11-04: one job each).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0027-engine-identities-and-packs.md">ADR 0027: engine, identities and packs</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0028-packs-stay-convertible-to-mcp.md">ADR 0028: packs stay convertible to MCP</seealso>
public sealed class ShopPricesPack : IAgentPack
{
    /// <summary>Builds the pack over the price tables it ships.</summary>
    public ShopPricesPack()
    {
        Toolsets =
        [
            new(ShopPricesToolset.ShopPrices.Wire(),
                "What a shopping list costs, estimated from Stats SA's average prices in the user's province",
                GuideFile: SkillFiles.Guide(ShopPricesToolset.ShopPrices.Wire()),
                MissingSettings: _ => [],
                ToolTypes: [typeof(ShopPriceTools)],
                CreateTools: _ => [new ShopPriceTools(PriceBook.Default)],
                // It reads a table in memory and changes nothing.
                ParallelSafe: true),
        ];
    }

    /// <inheritdoc />
    public string Name => ShopPricesToolset.ShopPrices.Wire();

    /// <inheritdoc />
    public IReadOnlyList<ToolsetDefinition> Toolsets { get; }

    /// <inheritdoc />
    public IFileProvider Prompts { get; } =
        new ManifestEmbeddedFileProvider(typeof(ShopPricesPack).Assembly, "Prompts");

    /// <inheritdoc />
    public IReadOnlyList<string> Requires => [];
}
