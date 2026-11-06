using LouisAgent.Packs.Recipes.Library;
using LouisAgent.Packs.Recipes.Library.Store;
using LouisAgent.Packs.Recipes.Recipes;
using MongoDB.Driver;

namespace LouisAgent.Packs.Recipes.IntegrationTests.Library.Store;

/// <summary>
/// The recipe store against a real MongoDB, when <c>BENCHMARKS_MONGO_URL</c> is set (CI's throwaway server): a kept
/// page comes back as the proper recipe it went in as, a page is only ever kept once and its first copy stays, one
/// recipe can be found by its id, and a site can be taken out. Each test gets a database of its own, dropped after.
/// </summary>
[Category("Integration")]
public class MongoRecipeStoreTests
{
    private static readonly DateTimeOffset Read = new(2026, 11, 5, 9, 30, 0, TimeSpan.Zero);

    /// <summary>Where Mongo runs, if this machine has one.</summary>
    private static string? Url => Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_URL");

    private string _database = null!;

    /// <summary>Skips the fixture without Mongo, and names a database only this test uses.</summary>
    [SetUp]
    public void NeedsMongo()
    {
        Outside.Tool(!string.IsNullOrEmpty(Url), "BENCHMARKS_MONGO_URL isn't set: the recipe store isn't tested here");
        _database = $"recipes_test_{Guid.NewGuid():N}";
    }

    /// <summary>Drops the test's database.</summary>
    [TearDown]
    public async Task DropTheTestDatabase()
    {
        if (string.IsNullOrEmpty(Url)) return;
        await new MongoClient(Url).DropDatabaseAsync(_database);
    }

    /// <summary>A recipe on a site, with this name, and every field the page can give.</summary>
    private static StoredRecipe Recipe(string url, string slug = "taste", string name = "Bobotie") => new(
        new Recipe(name, "4 people", TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(45), null,
            [new RecipeIngredient("Sauce", "1 kg mince"), new RecipeIngredient(null, "2 eggs")],
            [new RecipeStep(null, "Mix it."), new RecipeStep("To finish", "Bake it.")],
            new Uri("https://taste.co.za/bobotie.jpg"), "A cook", new Uri(url)),
        slug,
        Read);

    /// <summary>What goes in comes back the same, line by line, the date as UTC.</summary>
    [Test]
    public async Task AKeptPage_ComesBackAsItWentIn()
    {
        var store = MongoRecipeStore.Open(Url!, _database);
        var recipe = Recipe("https://taste.co.za/recipes/bobotie/");

        await store.KeepAsync(recipe, CancellationToken.None);

        var back = (await store.AllAsync(CancellationToken.None)).Single();
        Assert.That(back.Id, Is.EqualTo(recipe.Id));
        Assert.That(back.Page, Is.EqualTo(recipe.Page));
        Assert.That(back.SiteSlug, Is.EqualTo("taste"));
        Assert.That(back.ReadAt, Is.EqualTo(Read));
        Assert.That(back.Recipe.Name, Is.EqualTo("Bobotie"));
        Assert.That(back.Recipe.Yield, Is.EqualTo("4 people"));
        Assert.That(back.Recipe.PrepTime, Is.EqualTo(TimeSpan.FromMinutes(10)));
        Assert.That(back.Recipe.CookTime, Is.EqualTo(TimeSpan.FromMinutes(45)));
        Assert.That(back.Recipe.TotalTime, Is.Null);
        Assert.That(back.Recipe.Ingredients, Is.EqualTo(recipe.Recipe.Ingredients));
        Assert.That(back.Recipe.Steps, Is.EqualTo(recipe.Recipe.Steps));
        Assert.That(back.Recipe.Image, Is.EqualTo(recipe.Recipe.Image));
        Assert.That(back.Recipe.Author, Is.EqualTo("A cook"));
    }

    /// <summary>One recipe by its id, and an id we don't hold is nothing.</summary>
    [Test]
    public async Task OneRecipe_IsFoundByItsId()
    {
        var store = MongoRecipeStore.Open(Url!, _database);
        var recipe = Recipe("https://taste.co.za/recipes/bobotie/");
        await store.KeepAsync(recipe, CancellationToken.None);

        Assert.That((await store.FindAsync(recipe.Id, CancellationToken.None))?.Title, Is.EqualTo("Bobotie"));
        Assert.That(await store.FindAsync("0000000000000000", CancellationToken.None), Is.Null);
    }

    /// <summary>
    /// The first copy stays: a second read of the same page, spelled another way, changes nothing, and two reads at
    /// once can't fail each other.
    /// </summary>
    [Test]
    public async Task APage_IsKeptOnce_AndItsFirstCopyStays()
    {
        var store = MongoRecipeStore.Open(Url!, _database);
        var first = Recipe("https://www.taste.co.za/recipes/bobotie/", name: "first");

        await store.KeepAsync(first, CancellationToken.None);
        await Task.WhenAll(
            store.KeepAsync(Recipe("https://taste.co.za/recipes/bobotie", name: "second"), CancellationToken.None),
            store.KeepAsync(Recipe("http://taste.co.za/recipes/bobotie/", name: "third"), CancellationToken.None));

        Assert.That((await store.AllAsync(CancellationToken.None)).Select(recipe => recipe.Title),
            Is.EqualTo(new[] { "first" }));
    }

    /// <summary>Keeping says whether the page was new, however its link is spelled: the counter needs that.</summary>
    [Test]
    public async Task Keeping_SaysWhetherThePageWasNew()
    {
        var store = MongoRecipeStore.Open(Url!, _database);

        bool first = await store.KeepAsync(Recipe("https://www.taste.co.za/recipes/bobotie/"), CancellationToken.None);
        bool again = await store.KeepAsync(Recipe("http://taste.co.za/recipes/bobotie"), CancellationToken.None);

        Assert.That((first, again), Is.EqualTo((true, false)));
    }

    /// <summary>A site's owner can ask us to stop: its pages go, and the others stay.</summary>
    [Test]
    public async Task PurgingASite_TakesOutOnlyItsPages()
    {
        var store = MongoRecipeStore.Open(Url!, _database);
        await store.KeepAsync(Recipe("https://taste.co.za/a"), CancellationToken.None);
        await store.KeepAsync(Recipe("https://taste.co.za/b"), CancellationToken.None);
        await store.KeepAsync(Recipe("https://foodandhome.co.za/a", "foodandhome"), CancellationToken.None);

        await store.PurgeSiteAsync("taste", CancellationToken.None);

        Assert.That((await store.AllAsync(CancellationToken.None)).Select(recipe => recipe.SiteSlug),
            Is.EqualTo(new[] { "foodandhome" }));
    }
}
