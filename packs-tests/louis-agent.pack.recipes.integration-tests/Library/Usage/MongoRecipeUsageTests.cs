using LouisAgent.Packs.Recipes.Library.Usage;
using MongoDB.Driver;

namespace LouisAgent.Packs.Recipes.IntegrationTests.Library.Usage;

/// <summary>
/// The library's counter against a real MongoDB, when <c>BENCHMARKS_MONGO_URL</c> is set: fakes can't prove that two
/// adds at once both count. Each test gets its own database, dropped after.
/// </summary>
[Category("Integration")]
public class MongoRecipeUsageTests
{
    private static readonly DateOnly Today = new(2026, 11, 6);

    private static string? Url => Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_URL");

    private string _database = null!;

    /// <summary>Skips the fixture without Mongo, and names a database only this test uses.</summary>
    [SetUp]
    public void NeedsMongo()
    {
        Outside.Tool(!string.IsNullOrEmpty(Url), "BENCHMARKS_MONGO_URL isn't set: the library's counter isn't tested here");
        _database = $"usage_test_{Guid.NewGuid():N}";
    }

    /// <summary>Drops the test's database.</summary>
    [TearDown]
    public async Task DropTheTestDatabase()
    {
        if (string.IsNullOrEmpty(Url)) return;
        await new MongoClient(Url).DropDatabaseAsync(_database);
    }

    /// <summary>Two adds on one day sum, each number on its own, and the others stay at nothing.</summary>
    [Test]
    public async Task TwoAddsOnOneDay_Sum()
    {
        var usage = MongoRecipeUsage.Open(Url!, _database);

        await usage.AddAsync(RecipeUsageKind.Offered, 3, Today, CancellationToken.None);
        await usage.AddAsync(RecipeUsageKind.Offered, 2, Today, CancellationToken.None);
        await usage.AddAsync(RecipeUsageKind.Served, 1, Today, CancellationToken.None);

        Assert.That(await usage.DaysAsync(CancellationToken.None),
            Is.EqualTo(new[] { new RecipeUsageDay(Today, 0, 5, 1, 0) }));
    }

    /// <summary>Each kind has a number of its own.</summary>
    [Test]
    public async Task EachKind_HasItsOwnNumber()
    {
        var usage = MongoRecipeUsage.Open(Url!, _database);

        await usage.AddAsync(RecipeUsageKind.Kept, 1, Today, CancellationToken.None);
        await usage.AddAsync(RecipeUsageKind.Offered, 2, Today, CancellationToken.None);
        await usage.AddAsync(RecipeUsageKind.Served, 3, Today, CancellationToken.None);
        await usage.AddAsync(RecipeUsageKind.Failed, 4, Today, CancellationToken.None);

        Assert.That(await usage.DaysAsync(CancellationToken.None),
            Is.EqualTo(new[] { new RecipeUsageDay(Today, 1, 2, 3, 4) }));
    }

    /// <summary>Many callers at once on a day with no record yet all count: none is lost, and none fails.</summary>
    [Test]
    public async Task ManyCallersAtOnce_AllCount()
    {
        var usage = MongoRecipeUsage.Open(Url!, _database);

        await Task.WhenAll(Enumerable.Range(0, 50)
            .Select(_ => usage.AddAsync(RecipeUsageKind.Served, 1, Today, CancellationToken.None)));

        Assert.That((await usage.DaysAsync(CancellationToken.None)).Single().Served, Is.EqualTo(50));
    }

    /// <summary>Days come back newest first, across a month's end.</summary>
    [Test]
    public async Task Days_ComeBackNewestFirst()
    {
        var usage = MongoRecipeUsage.Open(Url!, _database);

        foreach (var day in new DateOnly[] { new(2026, 10, 31), new(2026, 11, 6), new(2026, 11, 1) })
            await usage.AddAsync(RecipeUsageKind.Kept, 1, day, CancellationToken.None);

        var days = await usage.DaysAsync(CancellationToken.None);
        Assert.That(days.Select(d => d.Day),
            Is.EqualTo(new DateOnly[] { new(2026, 11, 6), new(2026, 11, 1), new(2026, 10, 31) }));
    }

    /// <summary>With nothing counted yet there are no days, and no error.</summary>
    [Test]
    public async Task WithNothingCounted_ThereAreNoDays() =>
        Assert.That(await MongoRecipeUsage.Open(Url!, _database).DaysAsync(CancellationToken.None), Is.Empty);
}
