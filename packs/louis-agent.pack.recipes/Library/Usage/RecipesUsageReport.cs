using LouisAgent.Composition.Logging;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Packs.Reports;
using LouisAgent.Packs.Recipes.Messages;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Packs.Recipes.Library.Usage;

/// <summary>
/// The library's counter as a table for the admin page: a line adding up every day we hold, then the last 30 days,
/// newest first. It shows what the pack sees: whether an answer used a recipe stays with the eval's saved events.
/// </summary>
/// <remarks>
/// A plug-in of <see cref="IDataReport"/> (Plugin, Fowler), so the API and the page need no word about recipes. The
/// records stay when a day drops off the screen, so the top line still adds them up. A store that can't be read logs
/// and gives an empty table, as the contract asks, so one report can't blank the page.
/// </remarks>
/// <seealso href="https://martinfowler.com/eaaCatalog/plugin.html">Plugin (Fowler, PoEAA)</seealso>
internal sealed class RecipesUsageReport(IRecipeUsage usage) : IDataReport
{
    /// <summary>How many days show; the records themselves are all kept.</summary>
    private const int MostDays = 30;

    private static readonly ILogger Log = PackLog.For<RecipesUsageReport>();

    /// <inheritdoc />
    public string Name => "recipe-library";

    /// <inheritdoc />
    public string Description => MessageText.Format(RecipeReportMessage.Description);

    /// <inheritdoc />
    public async Task<ReportTable> ReadAsync(CancellationToken cancellationToken)
    {
        string[] columns =
        [
            MessageText.Format(RecipeReportMessage.Kept), MessageText.Format(RecipeReportMessage.Offered),
            MessageText.Format(RecipeReportMessage.Served), MessageText.Format(RecipeReportMessage.Failed)
        ];

        try
        {
            var days = await usage.DaysAsync(cancellationToken);
            return new ReportTable(columns, days.Count == 0 ? [] : [AllDays(days), .. days.Take(MostDays).Select(RowOf)]);
        }
        // The numbers aren't worth an error on the page; a cancel still goes through.
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            Log.Log(LogLevel.Warning, RecipeLogMessage.LibraryUnavailable, new { Reason = ex.GetType().Name });
            return new ReportTable(columns, []);
        }
    }

    /// <summary>The line adding up every day we hold, not only the ones showing.</summary>
    private static ReportRow AllDays(IReadOnlyList<RecipeUsageDay> days) =>
        new(MessageText.Format(RecipeReportMessage.AllDays),
        [
            days.Sum(day => day.Kept), days.Sum(day => day.Offered), days.Sum(day => day.Served),
            days.Sum(day => day.Failed)
        ]);

    /// <summary>One day's line, labelled as the day it's stored under.</summary>
    private static ReportRow RowOf(RecipeUsageDay day) =>
        new(RecipeUsageDocument.IdOf(day.Day), [day.Kept, day.Offered, day.Served, day.Failed]);
}
