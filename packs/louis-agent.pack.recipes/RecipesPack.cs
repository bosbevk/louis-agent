using LouisAgent.Composition.Packs;
using LouisAgent.Choices;
using LouisAgent.Core.Agent.Catalog;
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
        Toolsets =
        [
            new(RecipesToolset.Recipes.Wire(),
                "Recipes from South African recipe sites: search one, or read any recipe page",
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
                        ctx.Browser!)
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
}
