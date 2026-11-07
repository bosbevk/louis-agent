using System.Globalization;
using LouisAgent.Composition.Logging;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Packs.Screens;
using LouisAgent.Composition.Packs.Screens.Reading;
using LouisAgent.Packs.Recipes.Library.Usage;
using LouisAgent.Packs.Recipes.Messages;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Packs.Recipes.Library.Screens;

/// <summary>
/// The library's counter as an admin screen: a line adding up every day we hold, then the last 30 days, newest first.
/// It shows what the pack sees: whether an answer used a recipe stays with the eval's saved events.
/// </summary>
/// <remarks>
/// A plug-in of <see cref="IDataScreen" /> (Plugin, Fowler), so the API and the page need no word about recipes. The
/// records stay when a day drops off the screen, so the top line still adds them up. A store that can't be read logs
/// and gives an empty page, as the contract asks, so one screen can't break the others.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
/// <seealso href="https://martinfowler.com/eaaCatalog/plugin.html">Plugin (Fowler, PoEAA)</seealso>
internal sealed class RecipesUsageScreen(IRecipeUsage usage) : IDataScreen
{
    /// <summary>How many days show; the records themselves are all kept.</summary>
    private const int MostDays = 30;

    /// <summary>The all-days line's id, which no day can be.</summary>
    private const string AllDaysId = "all";

    private static readonly ILogger Log = PackLog.For<RecipesUsageScreen>();

    /// <inheritdoc />
    public ScreenSpec Spec { get; } = new(
        "recipe-library",
        MessageText.Format(RecipeScreenMessage.UsageTitle),
        MessageText.Format(RecipeScreenMessage.UsageDescription),
        [
            new ScreenColumn(MessageText.Format(RecipeScreenMessage.Day), ScreenCell.Text),
            new ScreenColumn(MessageText.Format(RecipeScreenMessage.Kept), ScreenCell.Number),
            new ScreenColumn(MessageText.Format(RecipeScreenMessage.Offered), ScreenCell.Number),
            new ScreenColumn(MessageText.Format(RecipeScreenMessage.Served), ScreenCell.Number),
            new ScreenColumn(MessageText.Format(RecipeScreenMessage.Failed), ScreenCell.Number)
        ],
        Searchable: false);

    /// <inheritdoc />
    public async Task<ScreenPage> ReadAsync(ScreenQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var days = await usage.DaysAsync(cancellationToken);
            if (days.Count == 0)
                return ScreenPage.Empty;

            ScreenRow[] rows = [AllDays(days), .. days.Take(MostDays).Select(RowOf)];
            // The most there can be is less than a page, so a later page is only ever empty.
            return new ScreenPage(query.Page == 1 ? rows : [], rows.Length);
        }
        // The numbers aren't worth an error on the page; a cancel still goes through.
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            Log.Log(LogLevel.Warning, RecipeLogMessage.LibraryUnavailable, new { Reason = ex.GetType().Name });
            return ScreenPage.Empty;
        }
    }

    /// <summary>The line adding up every day we hold, not only the ones showing.</summary>
    private static ScreenRow AllDays(IReadOnlyList<RecipeUsageDay> days) =>
        new(AllDaysId,
        [
            MessageText.Format(RecipeScreenMessage.AllDays), Number(days.Sum(day => day.Kept)),
            Number(days.Sum(day => day.Offered)), Number(days.Sum(day => day.Served)), Number(days.Sum(day => day.Failed))
        ]);

    /// <summary>One day's line, labelled as the day it's stored under.</summary>
    private static ScreenRow RowOf(RecipeUsageDay day)
    {
        var id = RecipeUsageDocument.IdOf(day.Day);
        return new ScreenRow(id, [id, Number(day.Kept), Number(day.Offered), Number(day.Served), Number(day.Failed)]);
    }

    /// <summary>A count as the table shows it, whatever this machine's culture.</summary>
    private static string Number(long count) => count.ToString(CultureInfo.InvariantCulture);
}
