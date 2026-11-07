using System.Globalization;
using LouisAgent.Composition.Logging;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Packs.Screens;
using LouisAgent.Composition.Packs.Screens.Acting;
using LouisAgent.Composition.Packs.Screens.Reading;
using LouisAgent.Packs.Recipes.Library.Excluded;
using LouisAgent.Packs.Recipes.Messages;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Packs.Recipes.Library.Screens;

/// <summary>
/// The pages nobody opens again, as an admin screen: those with no recipe, and those removed by hand. Clearing one gives
/// it another look, so the next read keeps it if it's a recipe.
/// </summary>
/// <remarks>A plug-in of <see cref="IDataScreen" /> (Plugin, Fowler), so the API and the page need no word about recipes.</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
/// <seealso href="https://martinfowler.com/eaaCatalog/plugin.html">Plugin (Fowler, PoEAA)</seealso>
internal sealed class ExcludedPagesScreen(IExcludedPages excluded) : IDataScreen
{
    private const string Clear = "clear";

    private static readonly ILogger Log = PackLog.For<ExcludedPagesScreen>();

    /// <inheritdoc />
    public ScreenSpec Spec { get; } = new(
        "excluded-pages",
        Say(RecipeScreenMessage.ExcludedTitle),
        Say(RecipeScreenMessage.ExcludedDescription),
        [
            new ScreenColumn(Say(RecipeScreenMessage.Page), ScreenCell.Link),
            new ScreenColumn(Say(RecipeScreenMessage.ExcludedAt), ScreenCell.Date)
        ],
        Searchable: true)
    {
        Actions = [new ScreenAction(Clear, Say(RecipeScreenMessage.ClearPage), ScreenActionOn.Row, Say(RecipeScreenMessage.ClearConfirm))]
    };

    /// <inheritdoc />
    public async Task<ScreenPage> ReadAsync(ScreenQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var found = (await excluded.AllAsync(cancellationToken))
                .Where(page => string.IsNullOrWhiteSpace(query.Search)
                               || page.Page.AbsoluteUri.Contains(query.Search.Trim(), StringComparison.OrdinalIgnoreCase))
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
    public async Task<ScreenAnswer> ActAsync(string action, string? rowId, CancellationToken cancellationToken)
    {
        if (action != Clear || rowId is null)
            return ScreenAnswer.NotOffered;

        return await excluded.RemoveAsync(rowId, cancellationToken)
            ? ScreenAnswer.Ok(Say(RecipeScreenMessage.Cleared))
            : ScreenAnswer.No(Say(RecipeScreenMessage.PageNotListed));
    }

    /// <summary>One page's line.</summary>
    private static ScreenRow RowOf(ExcludedPage page) =>
        new(page.Id, [page.Page.AbsoluteUri, page.At.ToString("O", CultureInfo.InvariantCulture)]);

    /// <summary>One of the screen's words.</summary>
    private static string Say(RecipeScreenMessage message) => MessageText.Format(message);
}
