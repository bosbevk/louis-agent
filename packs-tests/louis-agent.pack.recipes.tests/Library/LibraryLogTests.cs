using LouisAgent.Composition.Logging;
using LouisAgent.Packs.Recipes.Library;
using LouisAgent.Packs.Recipes.Sources;
using LouisAgent.Packs.Recipes.Sources.Gathering;
using LouisAgent.Packs.Recipes.Sources.Sites;
using LouisAgent.Packs.Recipes.Tests.Tools;
using LouisAgent.Packs.Recipes.Tests.Tools.Reading;
using LouisAgent.Packs.Recipes.Tests.Providers;
using LouisAgent.Packs.Recipes.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace LouisAgent.Packs.Recipes.Tests.Library;

/// <summary>
/// How we'd know the library is working outside an eval: the log says when a search offered its recipes, and when one
/// was served or couldn't be. It holds counts and ids, never what the user asked.
/// </summary>
[NonParallelizable]
public class LibraryLogTests
{
    private const string Query = "melktert vir ouma";
    private static readonly RecipeSite Taste = RecipeSites.Find("taste")!;
    private static readonly string OnOurSite = new Uri(Taste.Home, "recipes/melktert/").AbsoluteUri;

    private RecordingLog _log = null!;

    /// <summary>Every pack's lines come here for the test.</summary>
    [SetUp]
    public void Record()
    {
        _log = new RecordingLog();
        PackLog.Use(_log);
    }

    /// <summary>Back to nowhere, as a pack's tests have it.</summary>
    [TearDown]
    public void Stop()
    {
        PackLog.Use(NullLoggerFactory.Instance);
        _log.Dispose();
    }

    /// <summary>The shelf over this library, and the tools over the shelf.</summary>
    private static RecipeTools Tools(FakeRecipeLibrary library) =>
        new(new Dictionary<string, IRecipeSource>(), new FakeSiteBrowser(), new FakePhotoFetcher(),
            new RecipeShelf(library, library, [Taste], new FakeTimeProvider()));

    /// <summary>A search says how many of its hits were ours, and not what was searched for.</summary>
    [Test]
    public async Task ASearch_SaysHowManyWereOurs_AndNotWhatWasAsked()
    {
        var library = new FakeRecipeLibrary();
        library.Kept.Add(KeptRecipes.Of(OnOurSite, name: Query));
        var scatter = new RecipeScatter([new FakeRecipeSource("foodandhome")], new Random(1),
            new RecipeShelf(library, library, [Taste], new FakeTimeProvider()));

        await scatter.FindAsync(Query, 8);

        Assert.That(_log.Lines, Has.One.Contains("offered 1"));
        Assert.That(_log.Lines, Has.None.Contains(Query));
    }

    /// <summary>A search that found nothing of ours says nothing.</summary>
    [Test]
    public async Task ASearchWithNothingOfOurs_SaysNothing()
    {
        var library = new FakeRecipeLibrary();
        var scatter = new RecipeScatter([new FakeRecipeSource("foodandhome")], new Random(1),
            new RecipeShelf(library, library, [Taste], new FakeTimeProvider()));

        await scatter.FindAsync(Query, 8);

        Assert.That(_log.Lines, Is.Empty);
    }

    /// <summary>A recipe read from us says which one, and whose it is.</summary>
    [Test]
    public async Task ARead_SaysItWasServed()
    {
        var library = new FakeRecipeLibrary();
        var recipe = KeptRecipes.Of(OnOurSite);
        library.Kept.Add(recipe);

        await Tools(library).ReadRecipe(library.AddressOf(recipe).AbsoluteUri);

        Assert.That(_log.Lines, Has.One.Contains($"served {recipe.Id}").And.Contains(Taste.Slug));
    }

    /// <summary>A link of ours with no recipe behind it says so, which a working library shouldn't often do.</summary>
    [Test]
    public async Task AReadThatFindsNothing_SaysSo()
    {
        var library = new FakeRecipeLibrary();
        var gone = KeptRecipes.Of(OnOurSite);

        await Tools(library).ReadRecipe(library.AddressOf(gone).AbsoluteUri);

        Assert.That(_log.Lines, Has.One.Contains($"couldn't serve {gone.Id}"));
    }

    /// <summary>Keeps every line a pack writes.</summary>
    private sealed class RecordingLog : ILoggerFactory, ILogger
    {
        public List<string> Lines { get; } = [];

        public ILogger CreateLogger(string categoryName) => this;

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public void Dispose()
        {
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Lines.Add(formatter(state, exception));
    }
}
