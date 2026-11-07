using System.Globalization;
using LouisAgent.Composition.Logging;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Packs.Screens;
using LouisAgent.Composition.Packs.Screens.Acting;
using LouisAgent.Composition.Packs.Screens.Editing;
using LouisAgent.Composition.Packs.Screens.Reading;
using LouisAgent.Packs.Recipes.Library.Excluded;
using LouisAgent.Packs.Recipes.Library.Store;
using LouisAgent.Packs.Recipes.Messages;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Packs.Recipes.Library.Screens;

/// <summary>
/// The library's recipes as an admin screen: search them, open one, change any of its words, or remove it. A removed
/// page goes on the excluded list as well, so the next read doesn't simply keep it again.
/// </summary>
/// <remarks>
/// A plug-in of <see cref="IDataScreen" /> (Plugin, Fowler), so the API and the page need no word about recipes. A read
/// that fails is an empty page and an action that fails says so, as the contract asks, each logged.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
/// <seealso href="https://martinfowler.com/eaaCatalog/plugin.html">Plugin (Fowler, PoEAA)</seealso>
internal sealed class RecipesScreen(IRecipeStore store, IExcludedPages excluded, TimeProvider clock) : IDataScreen
{
    private const string Remove = "remove";

    private static readonly ILogger Log = PackLog.For<RecipesScreen>();

    /// <inheritdoc />
    public ScreenSpec Spec { get; } = new(
        "kept-recipes",
        Say(RecipeScreenMessage.RecipesTitle),
        Say(RecipeScreenMessage.RecipesDescription),
        [
            new ScreenColumn(Say(RecipeScreenMessage.Name), ScreenCell.Text),
            new ScreenColumn(Say(RecipeScreenMessage.Site), ScreenCell.Text),
            new ScreenColumn(Say(RecipeScreenMessage.Yield), ScreenCell.Text),
            new ScreenColumn(Say(RecipeScreenMessage.ReadAt), ScreenCell.Date),
            new ScreenColumn(Say(RecipeScreenMessage.Edited), ScreenCell.Date)
        ],
        Searchable: true)
    {
        Editable = true,
        Actions = [new ScreenAction(Remove, Say(RecipeScreenMessage.Remove), ScreenActionOn.Row, Say(RecipeScreenMessage.RemoveConfirm))]
    };

    /// <inheritdoc />
    public async Task<ScreenPage> ReadAsync(ScreenQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var found = (await store.AllAsync(cancellationToken))
                .Where(recipe => Matches(recipe, query.Search))
                .OrderByDescending(recipe => recipe.ReadAt)
                .ToList();
            var rows = found.Skip((query.Page - 1) * ScreenQuery.PageSize).Take(ScreenQuery.PageSize).Select(RowOf);
            return new ScreenPage([.. rows], found.Count);
        }
        // The list isn't worth an error on the page; a cancel still goes through.
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            Log.Log(LogLevel.Warning, RecipeLogMessage.LibraryUnavailable, new { Reason = ex.GetType().Name });
            return ScreenPage.Empty;
        }
    }

    /// <inheritdoc />
    public async Task<ScreenRecord?> ReadRowAsync(string rowId, CancellationToken cancellationToken) =>
        await store.FindAsync(rowId, cancellationToken) is { } stored ? RecipeForm.Of(stored) : null;

    /// <inheritdoc />
    public async Task<ScreenAnswer> SaveRowAsync(string rowId, ScreenValues values, CancellationToken cancellationToken)
    {
        if (await store.FindAsync(rowId, cancellationToken) is not { } stored)
            return ScreenAnswer.No(Say(RecipeScreenMessage.RecipeGone));

        var (recipe, errors) = RecipeForm.Read(stored, values);
        if (recipe is null)
            return new ScreenAnswer(false, Say(RecipeScreenMessage.NotSaved), errors);

        // Marked as edited, so the screen says so and nobody takes it for the page's own words.
        bool replaced = await store.ReplaceAsync(stored with { Recipe = recipe, EditedAt = clock.GetUtcNow() }, cancellationToken);
        return replaced ? ScreenAnswer.Ok(Say(RecipeScreenMessage.Saved)) : ScreenAnswer.No(Say(RecipeScreenMessage.RecipeGone));
    }

    /// <inheritdoc />
    public async Task<ScreenAnswer> ActAsync(string action, string? rowId, CancellationToken cancellationToken)
    {
        if (action != Remove || rowId is null)
            return ScreenAnswer.NotOffered;
        if (await store.FindAsync(rowId, cancellationToken) is not { } stored)
            return ScreenAnswer.No(Say(RecipeScreenMessage.RecipeGone));

        // Excluded first: if the second call fails the recipe is still there, rather than gone and ready to come back.
        await excluded.AddAsync(new ExcludedPage(stored.Page, clock.GetUtcNow()), cancellationToken);
        await store.RemoveAsync(rowId, cancellationToken);
        return ScreenAnswer.Ok(Say(RecipeScreenMessage.Removed));
    }

    /// <summary>Whether the words are in the recipe's name or its site's.</summary>
    private static bool Matches(StoredRecipe recipe, string? search) =>
        string.IsNullOrWhiteSpace(search)
        || recipe.Title.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)
        || recipe.SiteSlug.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>One recipe's line. Not edited is empty, as it's known; an unknown yield stays null and says so.</summary>
    private static ScreenRow RowOf(StoredRecipe recipe) => new(recipe.Id,
    [
        recipe.Title,
        recipe.SiteSlug,
        recipe.Recipe.Yield,
        recipe.ReadAt.ToString("O", CultureInfo.InvariantCulture),
        recipe.EditedAt?.ToString("O", CultureInfo.InvariantCulture) ?? ""
    ]);

    /// <summary>One of the screen's words.</summary>
    private static string Say(RecipeScreenMessage message) => MessageText.Format(message);
}
