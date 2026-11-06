using LouisAgent.Choices;
using LouisAgent.Packs.Recipes.Library;
using LouisAgent.Packs.Recipes.Library.Feed;
using LouisAgent.Packs.Recipes.Library.Store;
using LouisAgent.Packs.Recipes.Sources.Sites;

namespace LouisAgent.Packs.Recipes.Tests.Library;

/// <summary>
/// The library, published: every kept recipe, line by line, with its site's credit and link, only from sites still in
/// the table, and nothing about who asked. Each recipe also has its own address.
/// </summary>
public class RecipesFeedTests
{
    /// <summary>A store holding these, and failing if told to.</summary>
    private sealed class FakeStore(bool fails = false, params StoredRecipe[] recipes) : IRecipeStore
    {
        public Task<IReadOnlyList<StoredRecipe>> AllAsync(CancellationToken cancellationToken) =>
            fails ? throw new TimeoutException("no Mongo") : Task.FromResult<IReadOnlyList<StoredRecipe>>(recipes);

        public Task<StoredRecipe?> FindAsync(string id, CancellationToken cancellationToken) =>
            fails ? throw new TimeoutException("no Mongo") : Task.FromResult(recipes.FirstOrDefault(recipe => recipe.Id == id));

        public Task<bool> KeepAsync(StoredRecipe recipe, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task PurgeSiteAsync(string slug, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private static StoredRecipe Recipe(string slug) => KeptRecipes.Of($"https://example.com/{slug}/bobotie", slug, "Bobotie");

    /// <summary>The feed over a store, with these sites enabled.</summary>
    private static RecipesFeed Feed(IRecipeStore store, params string[] enabled) =>
        new(store, [.. enabled.Select(slug => RecipeSites.Find(slug)!)]);

    /// <summary>It goes by the pack's own name, as the shop prices' feed does.</summary>
    [Test]
    public void ItsName_IsThePacks() =>
        Assert.That(Feed(new NoRecipeStore()).Name, Is.EqualTo(RecipesToolset.Recipes.Wire()));

    /// <summary>A row carries the recipe line by line, its site by name, its own page and when we read it.</summary>
    [Test]
    public async Task ARecipe_CarriesItsLinesAndItsCredit()
    {
        string json = await Feed(new FakeStore(false, Recipe("taste")), "taste").ReadJsonAsync(default);

        var reply = RecipeFeedReply.Read(json);

        Assert.That(reply.Notice, Does.Contain("site"));
        var row = reply.Recipes.Single();
        Assert.That(row.Id, Is.EqualTo(Recipe("taste").Id));
        Assert.That(row.Page, Is.EqualTo("https://example.com/taste/bobotie"));
        Assert.That(row.Site, Is.EqualTo("taste"));
        Assert.That(row.SiteName, Is.EqualTo(RecipeSites.Find("taste")!.Name));
        Assert.That(row.Name, Is.EqualTo("Bobotie"));
        Assert.That(row.Ingredients.Select(line => line.Text), Is.EqualTo(new[] { "1 kg mince" }));
        Assert.That(row.Steps.Select(line => line.Text), Is.EqualTo(new[] { "Bake it." }));
    }

    /// <summary>A row read back is the same recipe, whichever fields the page gave.</summary>
    [Test]
    public void ARow_ReadsBackAsTheSameRecipe()
    {
        var recipe = Recipe("taste");

        var back = RecipeFeedRow.Read(RecipeFeedRow.Of(recipe, RecipeSites.Find("taste")!).Write()).ToRecipe();

        Assert.That(back.Recipe.Name, Is.EqualTo(recipe.Recipe.Name));
        Assert.That(back.Recipe.Yield, Is.EqualTo(recipe.Recipe.Yield));
        Assert.That(back.Recipe.PrepTime, Is.EqualTo(recipe.Recipe.PrepTime));
        Assert.That(back.Recipe.CookTime, Is.EqualTo(recipe.Recipe.CookTime));
        Assert.That(back.Recipe.TotalTime, Is.Null);
        Assert.That(back.Recipe.Image, Is.EqualTo(recipe.Recipe.Image));
        Assert.That(back.Recipe.Author, Is.EqualTo(recipe.Recipe.Author));
        Assert.That(back.Recipe.Ingredients, Is.EqualTo(recipe.Recipe.Ingredients));
        Assert.That(back.Recipe.Steps, Is.EqualTo(recipe.Recipe.Steps));
        Assert.That(back.Page, Is.EqualTo(recipe.Page));
        Assert.That(back.SiteSlug, Is.EqualTo(recipe.SiteSlug));
        Assert.That(back.ReadAt, Is.EqualTo(recipe.ReadAt));
    }

    /// <summary>Taking a site out of the table stops its recipes at once, even before its copies are deleted.</summary>
    [Test]
    public async Task ASiteNoLongerEnabled_IsNotPublished()
    {
        string json = await Feed(new FakeStore(false, Recipe("taste"), Recipe("foodandhome")), "taste").ReadJsonAsync(default);

        Assert.That(RecipeFeedReply.Read(json).Recipes.Select(row => row.Site), Is.EqualTo(new[] { "taste" }));
    }

    /// <summary>One recipe by its id, in the same shape as a row of the list.</summary>
    [Test]
    public async Task OneRecipe_IsReadByItsId()
    {
        var feed = Feed(new FakeStore(false, Recipe("taste")), "taste");

        string? json = await feed.ReadItemJsonAsync(Recipe("taste").Id, default);

        Assert.That(RecipeFeedRow.Read(json!).Name, Is.EqualTo("Bobotie"));
    }

    /// <summary>An unknown id, a site no longer in the table and a store that's down have no item to give.</summary>
    [Test]
    public async Task ARecipeWeCantGive_IsNoItem()
    {
        var hidden = Feed(new FakeStore(false, Recipe("foodandhome")), "taste");
        var down = Feed(new FakeStore(fails: true), "taste");

        Assert.That(await hidden.ReadItemJsonAsync("0000000000000000", default), Is.Null);
        Assert.That(await hidden.ReadItemJsonAsync(Recipe("foodandhome").Id, default), Is.Null);
        Assert.That(await down.ReadItemJsonAsync(Recipe("taste").Id, default), Is.Null);
    }

    /// <summary>What it says never carries who asked: there's no field for it.</summary>
    [Test]
    public void ARow_HasNoFieldForWhoAsked()
    {
        var names = typeof(RecipeFeedRow).GetProperties().Select(property => property.Name.ToLowerInvariant());

        Assert.That(names, Is.EquivalentTo(new[]
        {
            "id", "page", "site", "sitename", "name", "yield", "prepminutes", "cookminutes", "totalminutes",
            "ingredients", "steps", "image", "author", "readat"
        }));
    }

    /// <summary>A feed answers with what it has: a store that's down is an empty feed, never an error.</summary>
    [Test]
    public async Task AStoreThatIsDown_IsAnEmptyFeed()
    {
        string json = await Feed(new FakeStore(fails: true), "taste").ReadJsonAsync(default);

        Assert.That(RecipeFeedReply.Read(json).Recipes, Is.Empty);
    }
}
