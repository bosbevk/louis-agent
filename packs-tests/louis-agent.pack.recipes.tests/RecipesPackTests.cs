using LouisAgent.Composition.Web.Search;
using LouisAgent.Choices;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Config;
using LouisAgent.Core.Skills.Headers;
using LouisAgent.Core.Web.Search;
using LouisAgent.Packs.Recipes.Tests.Providers;
using LouisAgent.Packs.Recipes.Tools;

namespace LouisAgent.Packs.Recipes.Tests;

/// <summary>
/// Checks the recipes as a pack: its name, when its toolset can load, the tools it offers, and the guide that comes
/// with it.
/// </summary>
public class RecipesPackTests
{
    /// <summary>A search provider that finds nothing; these tests only need one to hand over.</summary>
    private sealed class NoSearch : IWebSearchProvider
    {
        /// <summary>What it calls itself.</summary>
        public string Name => "None";

        /// <summary>Answers every question with no results.</summary>
        public Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchQuery query,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<WebSearchResult>>([]);
    }

    /// <summary>The pack with the sites given, as the composition root builds it.</summary>
    private static RecipesPack Pack(string? sources = "foodandhome") =>
        new(new RecipesOptions { RecipeSources = sources });

    /// <summary>A context that has a browser, or does not.</summary>
    private static ToolsetContext Context(bool withBrowser) =>
        new(new AgentOptions(), new NoSearch(), WebBrowser: withBrowser ? new FakeSiteBrowser() : null);

    /// <summary>The pack and its one toolset go by the name <c>recipes</c>.</summary>
    [Test]
    public void ThePack_AndItsToolset_AreCalledRecipes()
    {
        var pack = Pack();

        Assert.That(pack.Name, Is.EqualTo(RecipesToolset.Recipes.Wire()));
        Assert.That(pack.Toolsets.Select(t => t.Name), Is.EqualTo(new[] { pack.Name }));
        Assert.That(pack.Requires, Is.Empty);
    }

    /// <summary>With no site named, the toolset is off and says to set <c>RECIPE_SOURCES</c>.</summary>
    [Test]
    public void WithoutRecipeSources_TheToolsetIsOff_AndNamesTheSetting()
    {
        var toolset = Pack(null).Toolsets[0];
        var context = Context(withBrowser: true);

        Assert.That(toolset.IsAvailable(context), Is.False);
        Assert.That(toolset.MissingSettings(context), Is.EqualTo(new[] { "RECIPE_SOURCES" }));
    }

    /// <summary>With no browser, the toolset is off and says to set <c>BROWSER_URL</c>.</summary>
    [Test]
    public void WithoutABrowser_TheToolsetIsOff_AndNamesTheBrowser()
    {
        var toolset = Pack().Toolsets[0];
        var context = Context(withBrowser: false);

        Assert.That(toolset.IsAvailable(context), Is.False);
        Assert.That(toolset.MissingSettings(context), Is.EqualTo(new[] { "BROWSER_URL" }));
    }

    /// <summary>With neither, it names both, and with both it is on.</summary>
    [Test]
    public void WithNeither_ItNamesBoth_AndWithBoth_ItIsOn()
    {
        Assert.That(Pack("").Toolsets[0].MissingSettings(Context(withBrowser: false)),
            Is.EqualTo(new[] { "RECIPE_SOURCES", "BROWSER_URL" }));
        Assert.That(Pack().Toolsets[0].IsAvailable(Context(withBrowser: true)), Is.True);
    }

    /// <summary>The tools are the recipe tools, built from the sites and the browser; calls run side by side.</summary>
    [Test]
    public void TheToolset_HoldsTheRecipeTools_AndRunsInParallel()
    {
        var toolset = Pack().Toolsets[0];

        Assert.That(toolset.ToolTypes, Is.EqualTo(new[] { typeof(RecipeTools) }));
        Assert.That(toolset.ParallelSafe, Is.True);
        Assert.That(toolset.CreateTools!(Context(withBrowser: true)),
            Has.Count.EqualTo(1).And.All.TypeOf<RecipeTools>());
    }

    /// <summary>The guide and the sites table are embedded in the pack, and the guide is the toolset's.</summary>
    [Test]
    public void TheGuide_AndTheSitesTable_AreEmbedded()
    {
        var pack = Pack();

        Assert.That(pack.Toolsets[0].GuideFile, Is.EqualTo("toolsets/recipes.md"));
        Assert.That(pack.Prompts.GetFileInfo("toolsets/recipes.md").Exists, Is.True);
        Assert.That(typeof(RecipesPack).Assembly.GetManifestResourceNames(),
            Has.Member("LouisAgent.Packs.Recipes.Sources.Sites.recipe-sites.md"));
    }

    /// <summary>The guide opens with the header every prompt has, named for its file, and holds no procedure.</summary>
    [Test]
    public void TheGuide_HasItsHeader_RoutesRecipesHere_AndHoldsNoProcedure()
    {
        using var reader = new StreamReader(Pack().Prompts.GetFileInfo("toolsets/recipes.md").CreateReadStream());
        string guide = reader.ReadToEnd();

        var header = PromptHeaders.Split("toolsets/recipes.md", guide).Header;
        Assert.That(header, Is.Not.Null);
        Assert.That(header!.Name, Is.EqualTo(RecipesToolset.Recipes.Wire()));
        Assert.That(guide, Does.Contain("Route here a request").And.Not.Contain("## Skill:"));
    }

    /// <summary>
    /// The guide says plainly that a hit can be a page that is not a recipe, and that the search finds only the
    /// enabled sites while ReadRecipe reads any public page.
    /// </summary>
    [Test]
    public void TheGuide_SaysAHitMayNotBeARecipe_AndWhatEachToolReaches()
    {
        using var reader = new StreamReader(Pack().Prompts.GetFileInfo("toolsets/recipes.md").CreateReadStream());
        string guide = reader.ReadToEnd();

        Assert.That(guide, Does.Contain("isn't a recipe").And.Contain("titles"));
        Assert.That(guide, Does.Contain("any public recipe page"));
    }
}
