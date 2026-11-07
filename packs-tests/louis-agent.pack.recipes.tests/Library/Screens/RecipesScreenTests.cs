using LouisAgent.Composition.Packs.Screens;
using LouisAgent.Composition.Packs.Screens.Editing;
using LouisAgent.Composition.Packs.Screens.Reading;
using LouisAgent.Packs.Recipes.Library;
using LouisAgent.Packs.Recipes.Library.Excluded;
using LouisAgent.Packs.Recipes.Library.Screens;
using LouisAgent.Packs.Recipes.Recipes;
using LouisAgent.Packs.Recipes.Tests.Tools.Reading;
using Microsoft.Extensions.Time.Testing;

namespace LouisAgent.Packs.Recipes.Tests.Library.Screens;

/// <summary>
/// The library's recipes as a screen: the list, the search, saving a form, and removing one, which also keeps its page
/// out. The excluded pages' own screen clears a page again.
/// </summary>
public class RecipesScreenTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 7, 8, 0, 0, TimeSpan.Zero);

    /// <summary>The excluded list in memory.</summary>
    private sealed class KeptList : IExcludedPages
    {
        public List<ExcludedPage> Pages { get; } = [];

        public Task<IReadOnlySet<string>> IdsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlySet<string>>(Pages.Select(page => page.Id).ToHashSet());

        public Task AddAsync(ExcludedPage page, CancellationToken cancellationToken)
        {
            Pages.Add(page);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ExcludedPage>> AllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ExcludedPage>>(Pages);

        public Task<bool> RemoveAsync(string id, CancellationToken cancellationToken) =>
            Task.FromResult(Pages.RemoveAll(page => page.Id == id) > 0);
    }

    /// <summary>A recipe on a site, read on a day in November.</summary>
    private static StoredRecipe Recipe(string slug, string name, int day = 1, string? yield = "4") => new(
        new Recipe(name, yield, null, null, null, [new RecipeIngredient(null, "1 egg")], [new RecipeStep(null, "Mix.")], null,
            null, new Uri($"https://example.com/{slug}/{name}/")),
        slug, new DateTimeOffset(2026, 11, day, 9, 0, 0, TimeSpan.Zero));

    /// <summary>The screen over a library holding these, and its two stores.</summary>
    private static (RecipesScreen Screen, FakeRecipeLibrary Library, KeptList Excluded) Over(params StoredRecipe[] recipes)
    {
        var library = new FakeRecipeLibrary();
        library.Kept.AddRange(recipes);
        var excluded = new KeptList();
        return (new RecipesScreen(library, excluded, new FakeTimeProvider(Now)), library, excluded);
    }

    /// <summary>What a form sends if nobody changes anything but the name.</summary>
    private static ScreenValues Renamed(StoredRecipe stored, string name)
    {
        var fields = RecipeForm.Of(stored).Fields;
        var values = fields.Where(field => !field.ReadOnly && field.Kind is not ScreenFieldKind.Rows)
            .ToDictionary(field => field.Name, field => field.Value);
        values["name"] = name;
        return new ScreenValues(values, fields.Where(field => field.Kind is ScreenFieldKind.Rows)
            .ToDictionary(field => field.Name, field => field.Rows));
    }

    /// <summary>It describes itself: searchable, editable, and removing takes a second click.</summary>
    [Test]
    public void ItSaysWhatItIs()
    {
        var spec = Over().Screen.Spec;

        Assert.That(spec.Name, Is.EqualTo("kept-recipes"));
        Assert.That(spec.Searchable, Is.True);
        Assert.That(spec.Editable, Is.True);
        Assert.That(spec.Actions.Single().Name, Is.EqualTo("remove"));
        Assert.That(spec.Actions.Single().Confirm, Is.Not.Null);
    }

    /// <summary>Search finds by title and by site, whatever the case.</summary>
    [TestCase("MELK", "melktert")]
    [TestCase("fooda", "bobotie")]
    public async Task Search_FindsByTitleAndBySite(string search, string found)
    {
        var (screen, _, _) = Over(Recipe("taste", "melktert"), Recipe("foodandhome", "bobotie"));

        var page = await screen.ReadAsync(new ScreenQuery(search, 1), CancellationToken.None);

        Assert.That(page.Rows.Single().Cells[0], Is.EqualTo(found));
        Assert.That(page.Total, Is.EqualTo(1));
    }

    /// <summary>The newest read comes first, and a page holds fifty, with the total of all of them.</summary>
    [Test]
    public async Task ThePages_AreNewestFirst_FiftyAtATime()
    {
        var recipes = Enumerable.Range(1, 60).Select(at => Recipe("taste", $"r{at}", 1 + (at % 28))).ToArray();
        var (screen, _, _) = Over(recipes);

        var first = await screen.ReadAsync(new ScreenQuery(null, 1), CancellationToken.None);
        var second = await screen.ReadAsync(new ScreenQuery(null, 2), CancellationToken.None);

        Assert.That(first.Rows, Has.Count.EqualTo(50));
        Assert.That(second.Rows, Has.Count.EqualTo(10));
        Assert.That(first.Total, Is.EqualTo(60));
    }

    /// <summary>No yield shows as unknown; not edited is empty, as that is known.</summary>
    [Test]
    public async Task NoYield_IsUnknown_AndNotEdited_IsEmpty()
    {
        var (screen, _, _) = Over(Recipe("taste", "melktert", yield: null));

        var cells = (await screen.ReadAsync(new ScreenQuery(null, 1), CancellationToken.None)).Rows.Single().Cells;

        Assert.That(cells[2], Is.Null);
        Assert.That(cells[4], Is.Empty);
    }

    /// <summary>A save takes its place and marks when, so the list says it was edited.</summary>
    [Test]
    public async Task ASave_ReplacesTheCopy_AndMarksItEdited()
    {
        var kept = Recipe("taste", "melktert");
        var (screen, library, _) = Over(kept);

        var answer = await screen.SaveRowAsync(kept.Id, Renamed(kept, "Melktert 2"), CancellationToken.None);

        Assert.That(answer.Done, Is.True);
        Assert.That(library.Kept.Single().Title, Is.EqualTo("Melktert 2"));
        Assert.That(library.Kept.Single().EditedAt, Is.EqualTo(Now));
        var cells = (await screen.ReadAsync(new ScreenQuery(null, 1), CancellationToken.None)).Rows.Single().Cells;
        Assert.That(cells[4], Is.Not.Empty);
    }

    /// <summary>A later read of the same page leaves the edit alone, as keeping only ever inserts.</summary>
    [Test]
    public async Task ALaterRead_DoesNotOverwriteTheEdit()
    {
        var kept = Recipe("taste", "melktert");
        var (screen, library, _) = Over(kept);
        await screen.SaveRowAsync(kept.Id, Renamed(kept, "Melktert 2"), CancellationToken.None);

        bool keptAgain = await library.KeepAsync(kept, CancellationToken.None);

        Assert.That(keptAgain, Is.False);
        Assert.That(library.Kept.Single().Title, Is.EqualTo("Melktert 2"));
    }

    /// <summary>A refusal names its field, and nothing changes.</summary>
    [Test]
    public async Task ARefusal_NamesItsField_AndNothingChanges()
    {
        var kept = Recipe("taste", "melktert");
        var (screen, library, _) = Over(kept);

        var answer = await screen.SaveRowAsync(kept.Id, Renamed(kept, " "), CancellationToken.None);

        Assert.That(answer.Done, Is.False);
        Assert.That(answer.FieldErrors.Keys, Is.EqualTo(new[] { "name" }));
        Assert.That(library.Kept.Single().Title, Is.EqualTo("melktert"));
    }

    /// <summary>A recipe that's gone says so, to read, to save and to remove.</summary>
    [Test]
    public async Task ARecipeThatsGone_SaysSo()
    {
        var kept = Recipe("taste", "melktert");
        var (screen, _, excluded) = Over();

        Assert.That(await screen.ReadRowAsync("nothing", CancellationToken.None), Is.Null);
        Assert.That((await screen.SaveRowAsync("nothing", Renamed(kept, "x"), CancellationToken.None)).Done, Is.False);
        Assert.That((await screen.ActAsync("remove", "nothing", CancellationToken.None)).Done, Is.False);
        Assert.That(excluded.Pages, Is.Empty);
    }

    /// <summary>Removing takes it out and keeps its page out, and the answer says when it leaves the feed.</summary>
    [Test]
    public async Task Removing_TakesItOut_AndKeepsItsPageOut()
    {
        var kept = Recipe("taste", "melktert");
        var (screen, library, excluded) = Over(kept);

        var answer = await screen.ActAsync("remove", kept.Id, CancellationToken.None);

        Assert.That(answer.Done, Is.True);
        Assert.That(answer.Message, Does.Contain("five minutes"));
        Assert.That(library.Kept, Is.Empty);
        Assert.That(excluded.Pages.Single().Id, Is.EqualTo(kept.Id));
    }

    /// <summary>An action it never offered does nothing.</summary>
    [Test]
    public async Task AnotherAction_DoesNothing()
    {
        var kept = Recipe("taste", "melktert");
        var (screen, library, _) = Over(kept);

        var answer = await screen.ActAsync("format", kept.Id, CancellationToken.None);

        Assert.That(answer.Done, Is.False);
        Assert.That(library.Kept, Has.Count.EqualTo(1));
    }

    /// <summary>A store that's down gives an empty page: a screen's read never throws.</summary>
    [Test]
    public async Task AStoreThatsDown_IsAnEmptyPage()
    {
        IDataScreen screen = new ExcludedPagesScreen(new FailingList());

        Assert.That(await screen.ReadAsync(new ScreenQuery(null, 1), CancellationToken.None), Is.EqualTo(ScreenPage.Empty));
    }

    /// <summary>A list whose server is down.</summary>
    private sealed class FailingList : IExcludedPages
    {
        public Task<IReadOnlySet<string>> IdsAsync(CancellationToken cancellationToken) => throw new TimeoutException();

        public Task AddAsync(ExcludedPage page, CancellationToken cancellationToken) => throw new TimeoutException();

        public Task<IReadOnlyList<ExcludedPage>> AllAsync(CancellationToken cancellationToken) => throw new TimeoutException();

        public Task<bool> RemoveAsync(string id, CancellationToken cancellationToken) => throw new TimeoutException();
    }

    /// <summary>The excluded list shows each page with when, and clearing one takes it off.</summary>
    [Test]
    public async Task ThePagesScreen_ListsThem_AndClearingTakesOneOff()
    {
        var list = new KeptList();
        var page = new ExcludedPage(new Uri("https://example.com/article/"), Now);
        await list.AddAsync(page, CancellationToken.None);
        var screen = new ExcludedPagesScreen(list);

        var rows = await screen.ReadAsync(new ScreenQuery(null, 1), CancellationToken.None);
        var answer = await screen.ActAsync("clear", page.Id, CancellationToken.None);
        var again = await screen.ActAsync("clear", page.Id, CancellationToken.None);

        Assert.That(rows.Rows.Single().Cells[0], Is.EqualTo("https://example.com/article/"));
        Assert.That(screen.Spec.Actions.Single().Confirm, Is.Not.Null);
        Assert.That(answer.Done, Is.True);
        Assert.That(list.Pages, Is.Empty);
        Assert.That(again.Done, Is.False);
    }

    /// <summary>The list can be searched by the page's address.</summary>
    [Test]
    public async Task ThePagesScreen_SearchesByAddress()
    {
        var list = new KeptList();
        await list.AddAsync(new ExcludedPage(new Uri("https://example.com/article/"), Now), CancellationToken.None);
        await list.AddAsync(new ExcludedPage(new Uri("https://example.com/about/"), Now), CancellationToken.None);

        var found = await new ExcludedPagesScreen(list).ReadAsync(new ScreenQuery("ABOUT", 1), CancellationToken.None);

        Assert.That(found.Rows.Single().Cells[0], Does.Contain("about"));
    }
}
