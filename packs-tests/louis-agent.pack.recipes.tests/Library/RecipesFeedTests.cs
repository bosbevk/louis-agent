using LouisAgent.Choices;
using LouisAgent.Packs.Recipes.Library;
using LouisAgent.Packs.Recipes.Library.Feed;
using LouisAgent.Packs.Recipes.Library.Store;
using LouisAgent.Packs.Recipes.Sources.Sites;

namespace LouisAgent.Packs.Recipes.Tests.Library;

/// <summary>
/// The library, published: every kept recipe with its site's credit and link, only from sites still in the table, and
/// nothing about who asked.
/// </summary>
public class RecipesFeedTests
{
    private static readonly DateTimeOffset Read = new(2026, 11, 5, 9, 30, 0, TimeSpan.Zero);

    /// <summary>A store holding these, and failing if told to.</summary>
    private sealed class FakeStore(bool fails = false, params StoredRecipe[] recipes) : IRecipeStore
    {
        public Task<IReadOnlyList<StoredRecipe>> AllAsync(CancellationToken cancellationToken) =>
            fails ? throw new TimeoutException("no Mongo") : Task.FromResult<IReadOnlyList<StoredRecipe>>(recipes);

        public Task KeepAsync(StoredRecipe recipe, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task PurgeSiteAsync(string slug, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private static StoredRecipe Recipe(string slug) =>
        new(new Uri($"https://example.com/{slug}/bobotie"), slug, "Bobotie", "# Bobotie\n1. Mince", Read);

    /// <summary>The feed over a store, with these sites enabled.</summary>
    private static RecipesFeed Feed(IRecipeStore store, params string[] enabled) =>
        new(store, [.. enabled.Select(slug => RecipeSites.Find(slug)!)]);

    /// <summary>It goes by the pack's own name, as the shop prices' feed does.</summary>
    [Test]
    public void ItsName_IsThePacks() =>
        Assert.That(Feed(new NoRecipeStore()).Name, Is.EqualTo(RecipesToolset.Recipes.Wire()));

    /// <summary>A recipe carries its print, its site by name, its own page and when we read it, in a shape we can read back.</summary>
    [Test]
    public async Task ARecipe_CarriesItsCreditAndLink()
    {
        string json = await Feed(new FakeStore(false, Recipe("taste")), "taste").ReadJsonAsync(default);

        var reply = RecipeFeedReply.Read(json);

        Assert.That(reply.Notice, Does.Contain("site"));
        var row = reply.Recipes.Single();
        Assert.That(row.Page, Is.EqualTo("https://example.com/taste/bobotie"));
        Assert.That(row.Site, Is.EqualTo("taste"));
        Assert.That(row.SiteName, Is.EqualTo(RecipeSites.Find("taste")!.Name));
        Assert.That(row.Print, Is.EqualTo("# Bobotie\n1. Mince"));
        Assert.That(row.ToRecipe(), Is.EqualTo(Recipe("taste")));
    }

    /// <summary>Taking a site out of the table stops its recipes at once, even before its copies are deleted.</summary>
    [Test]
    public async Task ASiteNoLongerEnabled_IsNotPublished()
    {
        string json = await Feed(new FakeStore(false, Recipe("taste"), Recipe("foodandhome")), "taste").ReadJsonAsync(default);

        Assert.That(RecipeFeedReply.Read(json).Recipes.Select(row => row.Site), Is.EqualTo(new[] { "taste" }));
    }

    /// <summary>What it says never carries who asked: there's no field for it.</summary>
    [Test]
    public void ARow_HasNoFieldForWhoAsked()
    {
        var names = typeof(RecipeFeedRow).GetProperties().Select(property => property.Name.ToLowerInvariant());

        Assert.That(names, Is.EquivalentTo(new[] { "page", "site", "sitename", "title", "print", "readat" }));
    }

    /// <summary>A feed answers with what it has: a store that's down is an empty feed, never an error.</summary>
    [Test]
    public async Task AStoreThatIsDown_IsAnEmptyFeed()
    {
        string json = await Feed(new FakeStore(fails: true), "taste").ReadJsonAsync(default);

        Assert.That(RecipeFeedReply.Read(json).Recipes, Is.Empty);
    }
}
