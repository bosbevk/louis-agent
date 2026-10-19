namespace LouisAgent.Packs.Shops;

using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Packs.Shops.Catalogues;
using LouisAgent.Packs.Shops.Providers;
using LouisAgent.Packs.Shops.Tools;
using LouisAgent.Core.Skills;
using Microsoft.Extensions.FileProviders;

/// <summary>
/// The shops as a pack (CL15, ADR 0027): one toolset, <c>shops</c>, with its guide, its own settings and its own store
/// cache (one per pack, so every engine this host builds shares it, F34). It loads only where <c>SHOP_CATALOGUES</c>
/// names a shop and <c>BROWSER_URL</c> gives the guarded browser, core's shared service, reached through the context.
/// Convertible to an MCP server (ADR 0028): its tools take plain data and return text, and its guide is prose.
/// </summary>
public sealed class ShopsPack : IAgentPack
{
    private readonly StoreSessionCache _stores;

    public ShopsPack(ShopsOptions options)
    {
        _stores = new StoreSessionCache(options.ShopStoreCache);
        Toolsets =
        [
            // F33: a shop's products from its own catalogue, through the guarded browser; off unless SHOP_CATALOGUES names a
            // shop (development and benchmarks first, ADR 0023).
            new("shops", "A shop's products and prices from its own catalogue (Checkers)",
                GuideFile: SkillFiles.Guide("shops"),
                MissingSettings: ctx => new[]
                {
                    ShopCatalogueFactory.Enabled(options.ShopCatalogues).Count > 0 ? null : "SHOP_CATALOGUES",
                    ctx.Browser is null ? "BROWSER_URL" : null,
                }.OfType<string>().ToList(),
                ToolType: typeof(ShopTools),
                CreateTools: ctx => [new ShopTools(ShopCatalogueFactory.Create(ShopCatalogueFactory.Enabled(options.ShopCatalogues), ctx.Browser!, _stores))],
                // F34: searches read a catalogue and change nothing; the browser opens at most four pages at once.
                ParallelSafe: true),
        ];
    }

    public string Name => "shops";

    public IReadOnlyList<ToolsetDefinition> Toolsets { get; }

    public IFileProvider Prompts { get; } = new ManifestEmbeddedFileProvider(typeof(ShopsPack).Assembly, "Prompts");

    public IReadOnlyList<string> Requires => [];
}
