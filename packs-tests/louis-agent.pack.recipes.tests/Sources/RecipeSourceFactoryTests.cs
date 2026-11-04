using LouisAgent.Core.Tools.Web.Search;
using LouisAgent.Packs.Recipes.Providers;
using LouisAgent.Packs.Recipes.Sources;
using LouisAgent.Packs.Recipes.Sources.Sites;
using LouisAgent.Packs.Recipes.Tests.Providers;

namespace LouisAgent.Packs.Recipes.Tests.Sources;

/// <summary>
/// Checks that the setting names the sites to turn on, and that each turned-on site gets the search its row says.
/// </summary>
public class RecipeSourceFactoryTests
{
    /// <summary>A search provider that finds nothing; these tests only need one to hand over.</summary>
    private sealed class NoSearch : IWebSearchProvider
    {
        public string Name => "None";

        /// <summary>Answers every question with no results.</summary>
        public Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchQuery query,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<WebSearchResult>>([]);
    }

    /// <summary>Each site the setting names is on once, however it is written and however often it is named.</summary>
    [Test]
    public void TheSetting_GivesEachNamedSiteOnce_WhateverTheCase() =>
        Assert.That(RecipeSourceFactory.Enabled("foodandhome, THEDALEYPLATE, foodandhome").Select(s => s.Slug),
            Is.EqualTo(new[] { "foodandhome", "thedaleyplate" }));

    /// <summary>A name that is not in the table turns nothing on and does not stop the others.</summary>
    [Test]
    public void ANameThatIsNoSite_IsIgnored() =>
        Assert.That(RecipeSourceFactory.Enabled("foodandhome,nosuchsite").Select(s => s.Slug),
            Is.EqualTo(new[] { "foodandhome" }));

    /// <summary>A star turns on every site in the table.</summary>
    [Test]
    public void AStar_GivesEverySite() =>
        Assert.That(RecipeSourceFactory.Enabled("*"), Has.Count.EqualTo(45));

    /// <summary>With no setting, or a blank one, no site is on.</summary>
    [Test]
    public void NothingSet_GivesNoSite([Values(null, "", "  ")] string? setting) =>
        Assert.That(RecipeSourceFactory.Enabled(setting), Is.Empty);

    /// <summary>
    /// A site searched through WordPress gets that search, and a site searched through the web gets the web one.
    /// </summary>
    [Test]
    public void AWordPressRow_GetsWordPressSearch_AndAWebRow_GetsSiteWebSearch()
    {
        var enabled = RecipeSourceFactory.Enabled("foodandhome,thedaleyplate");

        var sources = RecipeSourceFactory.Create(enabled, new FakeSiteBrowser(), new NoSearch());

        Assert.That(sources["foodandhome"], Is.TypeOf<WordPressSearch>());
        Assert.That(sources["thedaleyplate"], Is.TypeOf<SiteWebSearch>());
        Assert.That(sources["thedaleyplate"].Site.Slug, Is.EqualTo("thedaleyplate"));
    }

    /// <summary>With no site on, nothing is built.</summary>
    [Test]
    public void NoSiteEnabled_IsNoSource() =>
        Assert.That(RecipeSourceFactory.Create([], new FakeSiteBrowser(), new NoSearch()), Is.Empty);
}
