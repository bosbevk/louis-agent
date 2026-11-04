using LouisAgent.Choices;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Agent.Catalog.Imports;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Skills;
using LouisAgent.Packs.ShopPrices.Feeds;
using LouisAgent.Packs.ShopPrices.Imports;
using LouisAgent.Packs.ShopPrices.Prices.StatsSa;
using LouisAgent.Packs.ShopPrices.Prices.Store;
using LouisAgent.Packs.ShopPrices.Tools;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Packs.ShopPrices;

/// <summary>
/// Shop prices as a pack: one toolset, <c>shop-prices</c>, that estimates what a shopping list costs from Stats SA's
/// average prices, one feed of the same name that publishes those tables for anyone to read (ADR 0070), and one
/// import of the same name that takes Stats SA's spreadsheets from the admin portal (F55). It needs no setting and no browser, so it loads wherever it is named: with SHOP_PRICES_MONGO_URL
/// it reads the prices from MongoDB, seeded from the tables it ships; without it, from those tables in memory.
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
    /// <summary>Builds the pack over the price store its settings name, or the tables it ships when they name none.</summary>
    public ShopPricesPack(ShopPricesOptions options)
    {
        IPriceStore store = options.MongoUrl is { } url
            ? MongoPriceStore.Open(url, options.MongoDatabase)
            : new ShippedPrices();
        // One cache for every session, so the store is read once an hour, whatever the number of engines.
        var books = new PriceBookCache(store, TimeProvider.System);
        Toolsets =
        [
            new(ShopPricesToolset.ShopPrices.Wire(),
                "What a shopping list costs, estimated from Stats SA's average prices in the user's province",
                GuideFile: SkillFiles.Guide(ShopPricesToolset.ShopPrices.Wire()),
                MissingSettings: _ => [],
                ToolTypes: [typeof(ShopPriceTools)],
                CreateTools: _ => [new ShopPriceTools(books)],
                // It reads the prices and changes nothing.
                ParallelSafe: true),
        ];
        // The same tables, published for anyone to read (ADR 0070), through the same cache.
        Feeds = [new ShopPricesFeed(books)];
        // Stats SA's spreadsheets, uploaded from the admin portal into the same store (F55).
        Imports = [new StatsSaImport(store, books, StatsSaNames.Default)];
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

    /// <inheritdoc />
    public IReadOnlyList<IDataFeed> Feeds { get; }

    /// <inheritdoc />
    public IReadOnlyList<IDataImport> Imports { get; }
}
