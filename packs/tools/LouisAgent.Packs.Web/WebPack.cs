using LouisAgent.Choices;
using LouisAgent.Composition.Packs;
using LouisAgent.Packs.Web.Currency;
using LouisAgent.Packs.Web.Quotes;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Packs.Web;

/// <summary>The web as tools: search, read a page, check a quote, convert the money it names.</summary>
/// <remarks>
/// Only the tools. The browser, the search providers and the guards around every fetch are the engine's, handed over
/// through <see cref="IPackContext"/>, so this pack can't reach an internal address or skip a site's robots.txt.
/// </remarks>
public sealed class WebPack : IAgentPack
{
    /// <inheritdoc/>
    public string Name => WebToolset.Web.Wire();

    /// <inheritdoc/>
    public IReadOnlyList<PackToolset> Toolsets { get; } =
    [
        new(WebToolset.Web.Wire(), "Search the web and fetch web pages",
            GuideFile: PackToolset.Guide(WebToolset.Web.Wire()),
            MissingSettings: _ => [],
            // Three classes: finding and reading, a quote's provenance, currency. One fetcher for the two that fetch.
            ToolTypes: [typeof(WebTools), typeof(QuoteTools), typeof(CurrencyTools)],
            CreateTools: context =>
            {
                var fetcher = context.NewFetcher();
                return
                [
                    new WebTools(context.WebSearch, fetcher, context.Renderer),
                    new QuoteTools(fetcher, context.Renderer),
                    new CurrencyTools(context.CurrencyRates)
                ];
            },
            ParallelSafe: true),
    ];

    /// <inheritdoc/>
    public bool ComesBeforeTheEngine => true;

    /// <inheritdoc/>
    public IFileProvider Prompts { get; } = EmbeddedPrompts.Of(typeof(WebPack).Assembly);

    /// <inheritdoc/>
    public IReadOnlyList<string> Requires => [];
}

/// <summary>The name this pack's toolset goes by (ADR 0043).</summary>
public enum WebToolset
{
    Web,
}
