using LouisAgent.Packs.Recipes.Library;
using LouisAgent.Packs.Recipes.Library.Excluded;
using MongoDB.Driver;

namespace LouisAgent.Packs.Recipes.IntegrationTests.Library.Excluded;

/// <summary>
/// The excluded pages against a real MongoDB, when <c>BENCHMARKS_MONGO_URL</c> is set: a listed page comes back by its
/// id, however its link is spelled, and listing it twice at once fails nothing. Each test gets its own database, dropped
/// after.
/// </summary>
[Category("Integration")]
public class MongoExcludedPagesTests
{
    private static string? Url => Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_URL");

    private string _database = null!;

    /// <summary>Skips the fixture without Mongo, and names a database only this test uses.</summary>
    [SetUp]
    public void NeedsMongo()
    {
        Outside.Tool(!string.IsNullOrEmpty(Url), "BENCHMARKS_MONGO_URL isn't set: the excluded pages aren't tested here");
        _database = $"excluded_test_{Guid.NewGuid():N}";
    }

    /// <summary>Drops the test's database.</summary>
    [TearDown]
    public async Task DropTheTestDatabase()
    {
        if (string.IsNullOrEmpty(Url)) return;
        await new MongoClient(Url).DropDatabaseAsync(_database);
    }

    /// <summary>A listed page comes back by its id, the same however the link is spelled.</summary>
    [Test]
    public async Task AListedPage_ComesBackByItsId()
    {
        var list = MongoExcludedPages.Open(Url!, _database);

        await list.AddAsync(new ExcludedPage(new Uri("https://www.taste.co.za/article/"), DateTimeOffset.UnixEpoch), CancellationToken.None);

        var ids = await list.IdsAsync(CancellationToken.None);
        Assert.That(ids, Does.Contain(RecipeAddress.IdOf(new Uri("http://taste.co.za/article"))));
        Assert.That(ids, Has.Count.EqualTo(1));
    }

    /// <summary>Two cooks finding the same page at once can't fail each other, and it's listed once.</summary>
    [Test]
    public async Task APage_IsListedOnce()
    {
        var list = MongoExcludedPages.Open(Url!, _database);
        var page = new ExcludedPage(new Uri("https://taste.co.za/article/"), DateTimeOffset.UnixEpoch);

        await Task.WhenAll(list.AddAsync(page, CancellationToken.None), list.AddAsync(page, CancellationToken.None));

        Assert.That(await list.IdsAsync(CancellationToken.None), Has.Count.EqualTo(1));
    }
}
