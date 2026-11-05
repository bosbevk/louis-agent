using LouisAgent.Packs.Recipes.Library;
using LouisAgent.Packs.Recipes.Library.Store;
using MongoDB.Driver;

namespace LouisAgent.Packs.Recipes.IntegrationTests.Library.Store;

/// <summary>
/// The recipe store against a real MongoDB, when <c>BENCHMARKS_MONGO_URL</c> is set (CI's throwaway server): a kept page
/// comes back as it went in, a page is only ever kept once and its first copy stays, and a site can be taken out. Each
/// test gets a database of its own, dropped after.
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

    /// <summary>A recipe on a site, with this text.</summary>
    private static StoredRecipe Recipe(string url, string slug = "taste", string print = "# Bobotie\n1. Mince") =>
        new(new Uri(url), slug, "Bobotie", print, Read);

    /// <summary>What goes in comes back the same, the date as UTC.</summary>
    [Test]
    public async Task AKeptPage_ComesBackAsItWentIn()
    {
        var store = MongoRecipeStore.Open(Url!, _database);
        var recipe = Recipe("https://taste.co.za/recipes/bobotie/");

        await store.KeepAsync(recipe, CancellationToken.None);

        Assert.That(await store.AllAsync(CancellationToken.None), Is.EqualTo(new[] { recipe }));
    }

    /// <summary>
    /// The first copy stays: a second read of the same page, spelled another way, changes nothing, and two reads at once
    /// can't fail each other.
    /// </summary>
    [Test]
    public async Task APage_IsKeptOnce_AndItsFirstCopyStays()
    {
        var store = MongoRecipeStore.Open(Url!, _database);
        var first = Recipe("https://www.taste.co.za/recipes/bobotie/", print: "first");

        await store.KeepAsync(first, CancellationToken.None);
        await Task.WhenAll(
            store.KeepAsync(Recipe("https://taste.co.za/recipes/bobotie", print: "second"), CancellationToken.None),
            store.KeepAsync(Recipe("http://taste.co.za/recipes/bobotie/", print: "third"), CancellationToken.None));

        Assert.That((await store.AllAsync(CancellationToken.None)).Select(recipe => recipe.Print),
            Is.EqualTo(new[] { "first" }));
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
