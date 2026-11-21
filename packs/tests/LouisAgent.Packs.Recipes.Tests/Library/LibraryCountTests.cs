using LouisAgent.Packs.Recipes.Library;
using LouisAgent.Packs.Recipes.Library.Usage;
using LouisAgent.Packs.Recipes.Sources.Gathering;
using LouisAgent.Packs.Recipes.Sources.Sites;
using LouisAgent.Packs.Recipes.Tests.Library.Usage;
using LouisAgent.Packs.Recipes.Tests.Tools;
using LouisAgent.Packs.Recipes.Tests.Tools.Reading;
using Microsoft.Extensions.Time.Testing;

namespace LouisAgent.Packs.Recipes.Tests.Library;

/// <summary>
/// The library's counter as the tools drive it: what was kept, offered, served and couldn't be. It's a small write
/// beside the work, so a counter that's down costs the cook nothing, and a trial that leaves the library alone counts
/// nothing.
/// </summary>
public class LibraryCountTests
{
    private const string Query = "melktert";

    private static readonly RecipeSite Taste = RecipeSites.Find("taste")!;
    private static readonly string OnOurSite = new Uri(Taste.Home, "recipes/melktert/").AbsoluteUri;
    private static readonly DateOnly Today = new(2026, 11, 6);
    private static readonly DateTimeOffset Noon = new(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);

    /// <summary>The shelf over this library and counter, on a clock at noon.</summary>
    private static RecipeShelf Shelf(FakeRecipeLibrary library, FakeRecipeUsage usage, Func<bool>? skip = null,
        DateTimeOffset? now = null) =>
        new(library, library, [Taste], new FakeTimeProvider(now ?? Noon), skip, usage: usage);

    /// <summary>A recipe of ours, kept in the library.</summary>
    private static StoredRecipe Held(FakeRecipeLibrary library)
    {
        var recipe = KeptRecipes.Of(OnOurSite, name: Query);
        library.Kept.Add(recipe);
        return recipe;
    }

    /// <summary>A recipe read from us counts as served, on today's record.</summary>
    [Test]
    public async Task ARead_CountsAsServed()
    {
        var (library, usage) = (new FakeRecipeLibrary(), new FakeRecipeUsage());
        var held = Held(library);

        await Shelf(library, usage).ReadAsync(library.AddressOf(held), CancellationToken.None);

        Assert.That(usage.Added, Is.EqualTo(new[] { (RecipeUsageKind.Served, 1L, Today) }));
    }

    /// <summary>A read we couldn't serve counts as failed, whether it's gone or the library is down.</summary>
    [Test]
    public async Task AReadWeCouldntServe_CountsAsFailed()
    {
        var (down, usage) = (new FakeRecipeLibrary(fails: true), new FakeRecipeUsage());

        var recipe = await Shelf(down, usage).ReadAsync(down.AddressOf(KeptRecipes.Of(OnOurSite)), CancellationToken.None);

        Assert.That(recipe, Is.Null);
        Assert.That(usage.Added, Is.EqualTo(new[] { (RecipeUsageKind.Failed, 1L, Today) }));
    }

    /// <summary>A page kept twice counts once: the second time it was nothing new.</summary>
    [Test]
    public async Task APageKeptTwice_CountsOnce()
    {
        var (library, usage) = (new FakeRecipeLibrary(), new FakeRecipeUsage());
        var recipe = KeptRecipes.Of(OnOurSite).Recipe;
        var shelf = Shelf(library, usage);

        await shelf.KeepAsync(recipe, CancellationToken.None);
        await shelf.KeepAsync(recipe, CancellationToken.None);

        Assert.That(usage.Added, Is.EqualTo(new[] { (RecipeUsageKind.Kept, 1L, Today) }));
    }

    /// <summary>A page from a site we don't list isn't kept, so it isn't counted.</summary>
    [Test]
    public async Task APageFromElsewhere_CountsNothing()
    {
        var (library, usage) = (new FakeRecipeLibrary(), new FakeRecipeUsage());

        await Shelf(library, usage).KeepAsync(KeptRecipes.Of("https://example.org/recipes/pie/").Recipe,
            CancellationToken.None);

        Assert.That(usage.Added, Is.Empty);
    }

    /// <summary>A search counts the hits of ours it showed, not how many it held.</summary>
    [Test]
    public async Task ASearch_CountsTheHitsItShowed()
    {
        var (library, usage) = (new FakeRecipeLibrary(), new FakeRecipeUsage());
        library.Kept.AddRange(Enumerable.Range(1, 6)
            .Select(i => KeptRecipes.Of(new Uri(Taste.Home, $"recipes/melktert-{i}/").AbsoluteUri, name: $"{Query} {i}")));
        var scatter = new RecipeScatter([new FakeRecipeSource("foodandhome")], new Random(1), Shelf(library, usage));

        var gather = await scatter.FindAsync(Query, 2);

        long shown = gather.Hits.Count(hit => hit.Hit.Url.Host == "library.example");
        Assert.That(shown, Is.EqualTo(2), "two of the six make the list");
        Assert.That(usage.Added, Is.EqualTo(new[] { (RecipeUsageKind.Offered, shown, Today) }));
    }

    /// <summary>A search that showed nothing of ours has nothing to add.</summary>
    [Test]
    public async Task ASearchWithNothingOfOurs_CountsNothing()
    {
        var (library, usage) = (new FakeRecipeLibrary(), new FakeRecipeUsage());
        var scatter = new RecipeScatter([new FakeRecipeSource("foodandhome")], new Random(1), Shelf(library, usage));

        await scatter.FindAsync(Query, 8);

        Assert.That(usage.Added, Is.Empty);
    }

    /// <summary>A counter that's down costs nothing: the read and the keep both still happen.</summary>
    [Test]
    public async Task ACounterThatsDown_CostsTheCookNothing()
    {
        var (library, usage) = (new FakeRecipeLibrary(), new FakeRecipeUsage(fails: true));
        var held = Held(library);
        var shelf = Shelf(library, usage);

        var read = await shelf.ReadAsync(library.AddressOf(held), CancellationToken.None);
        await shelf.KeepAsync(KeptRecipes.Of(new Uri(Taste.Home, "recipes/bobotie/").AbsoluteUri).Recipe,
            CancellationToken.None);

        Assert.That(read, Is.EqualTo(held));
        Assert.That(library.Kept, Has.Count.EqualTo(2));
    }

    /// <summary>A trial that leaves the library alone leaves its counter alone too.</summary>
    [Test]
    public async Task ATrialThatSkipsTheLibrary_CountsNothing()
    {
        var (library, usage) = (new FakeRecipeLibrary(), new FakeRecipeUsage());
        var held = Held(library);
        var shelf = Shelf(library, usage, () => true);

        await shelf.ReadAsync(library.AddressOf(held), CancellationToken.None);
        await shelf.KeepAsync(KeptRecipes.Of(new Uri(Taste.Home, "recipes/bobotie/").AbsoluteUri).Recipe,
            CancellationToken.None);
        await shelf.OfferedAsync(3, CancellationToken.None);

        Assert.That(usage.Added, Is.Empty);
    }

    /// <summary>Days are UTC: half past midnight in Stellenbosch still counts on the day before.</summary>
    [Test]
    public async Task ADay_IsAUtcDay()
    {
        var (library, usage) = (new FakeRecipeLibrary(), new FakeRecipeUsage());
        var held = Held(library);
        var lateNight = new DateTimeOffset(2026, 11, 7, 0, 30, 0, TimeSpan.FromHours(2));

        await Shelf(library, usage, now: lateNight).ReadAsync(library.AddressOf(held), CancellationToken.None);

        Assert.That(usage.Added.Single().Day, Is.EqualTo(Today));
    }
}
