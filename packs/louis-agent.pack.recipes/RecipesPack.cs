using LouisAgent.Composition.Packs;
using LouisAgent.Choices;
using LouisAgent.Packs.Recipes.Library;
using LouisAgent.Packs.Recipes.Library.Excluded;
using LouisAgent.Packs.Recipes.Library.Feed;
using LouisAgent.Packs.Recipes.Library.Store;
using LouisAgent.Packs.Recipes.Sources;
using LouisAgent.Packs.Recipes.Tools;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Packs.Recipes;

/// <summary>
/// Recipes as a pack: one toolset, <c>recipes</c>, with its guide and its own settings. It loads only where
/// <c>RECIPE_SOURCES</c> names a site and <c>BROWSER_URL</c> gives the guarded browser, which core shares and the
/// context hands over.
/// </summary>
/// <remarks>
/// The same shape as every pack (ADR 0027): the pack is a list of toolsets and the files the model reads, and it
/// names no identity and no other pack, so it can be served as an MCP server unchanged (ADR 0028). Its tools take plain
/// data and return text, and its guide is prose.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0027-engine-identities-and-packs.md">ADR 0027: engine, identities and packs</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0028-packs-stay-convertible-to-mcp.md">ADR 0028: packs stay convertible to MCP</seealso>
public sealed class RecipesPack : IAgentPack
{
    /// <summary>Builds the pack from the sites the deployment turned on.</summary>
    public RecipesPack(RecipesOptions options)
    {
        IRecipeStore store = options.MongoUrl is { } url
            ? MongoRecipeStore.Open(url, options.MongoDatabase)
            : new NoRecipeStore();
        IExcludedPages excluded = options.MongoUrl is { } listUrl
            ? MongoExcludedPages.Open(listUrl, options.MongoDatabase)
            : new NoExcludedPages();
        var enabled = RecipeSourceFactory.Enabled(options.RecipeSources);
        // With no database there's nothing to publish, so there's no feed to list.
        Feeds = options.MongoUrl is null ? [] : [new RecipesFeed(store, enabled)];
        // The cook reads the library the way anyone would: over HTTP, from the open feed.
        IRecipeLibrary library = options.LibraryUrl is { } api
            ? new FeedRecipeLibrary(new HttpClient(), api, TimeProvider.System)
            : new NoRecipeLibrary();
        Toolsets =
        [
            new(RecipesToolset.Recipes.Wire(),
                "Recipes from South African recipe sites: search a few at once or one by name, or read any recipe page",
                GuideFile: PackToolset.Guide(RecipesToolset.Recipes.Wire()),
                MissingSettings: ctx => new[]
                {
                    RecipeSourceFactory.Enabled(options.RecipeSources).Count > 0 ? null : "RECIPE_SOURCES",
                    ctx.Browser is null ? "BROWSER_URL" : null,
                }.OfType<string>().ToList(),
                ToolTypes: [typeof(RecipeTools)],
                CreateTools: ctx =>
                [
                    new RecipeTools(
                        RecipeSourceFactory.Create(RecipeSourceFactory.Enabled(options.RecipeSources), ctx.Browser!,
                            ctx.WebSearch),
                        ctx.Browser!, ctx.NewFetcher(),
                        // An eval's trial leaves the library alone, unless a run that measures it says otherwise.
                        new RecipeShelf(library, store, enabled, TimeProvider.System,
                            () => ctx.InATrial && !options.LibraryInEvals, excluded))
                ],
                // Searches and page reads change nothing, and the browser opens at most four pages at once.
                ParallelSafe: true),
        ];
    }

    /// <inheritdoc />
    public string Name => RecipesToolset.Recipes.Wire();

    /// <inheritdoc />
    public IReadOnlyList<PackToolset> Toolsets { get; }

    /// <inheritdoc />
    public IFileProvider Prompts { get; } = new ManifestEmbeddedFileProvider(typeof(RecipesPack).Assembly, "Prompts");

    /// <inheritdoc />
    public IReadOnlyList<string> Requires => [];

    /// <inheritdoc />
    public IReadOnlyList<IDataFeed> Feeds { get; }
}
