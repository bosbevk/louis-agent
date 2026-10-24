using LouisAgent.Evals.Puls.Scoring;
using LouisAgent.Evals.Puls.Transcripts;

namespace LouisAgent.Evals.Puls.Cook;

/// <summary>
/// The cook check's score function (the Python's <c>cook/run_check.py</c> <c>score</c>): one conversation's turns as numbers.
/// A meal goes to the cook when the mode has it; otherwise, and for the control question, Puls answers itself. An errored
/// first turn is never right. Behaviour is the Python's as it is today; the no-shop rule changes in step 3.3.
/// </summary>
public static class CookScorer
{
    private const string CookRole = "puls-cook";

    /// <summary>The conversations whose town has no shop the cook has a tool for (Prince Albert: no Checkers).</summary>
    private static readonly IReadOnlySet<string> NoShop = new HashSet<string> { "cook-no-shop" };

    /// <summary>The shops' sites, sorted and without repeats (the Python's SHOPS).</summary>
    private static readonly string[] SiteHosts = Shops.Hosts.Select(h => h.Host).Distinct().OrderBy(h => h, StringComparer.Ordinal).ToArray();

    public static CookScores Score(string task, IReadOnlyList<TurnView> turns, bool cookOn)
    {
        var first = turns[0];
        var follow = turns.Count > 1 ? turns[1] : null;

        var price = Prices.Priced(first.Answer);
        int? items = price?.Listed, priced = price?.Priced, guessed = price?.Guessed, perKg = price?.PerKg;
        int noShopRows = 0, wrongStore = 0;
        if (NoShop.Contains(task) && items is > 0)
        {
            wrongStore = priced ?? 0;
            priced = 0;
            noShopRows = items.Value - wrongStore - (guessed ?? 0) - (perKg ?? 0);
        }

        var allCalls = turns.SelectMany(t => t.Calls.Concat(t.SpecialistCalls)).ToList();
        var fetched = ToolHosts.Of(allCalls);
        var sources = fetched.Where(h => !Shops.MapHosts.Any(map => h.EndsWith(map, StringComparison.Ordinal))).ToList();
        var local = sources.Where(h => h.EndsWith(".za", StringComparison.Ordinal)).ToList();
        var shops = fetched.Where(h => SiteHosts.Any(site => h.EndsWith(site, StringComparison.Ordinal))).ToList();
        string? expected = task.StartsWith("cook", StringComparison.Ordinal) && cookOn ? CookRole : null;
        var shopCalls = allCalls.Where(c => Shops.ShopTools.Contains(c.Name)).ToList();
        var results = turns.SelectMany(t => t.Results.Concat(t.SpecialistResults)).ToList();

        string? followUp = follow is null ? null
            : follow.Continued is not null ? "continued"
            : follow.Role is not null ? "new sub-session"
            : "answered itself";

        return new CookScores(
            Role: first.Role,
            RoutedRight: first.Error is null && first.Role == expected,
            AnsweredItself: first.Role is null && first.Error is null,
            Hosts: fetched,
            LocalHosts: local,
            ShopHosts: shops,
            SourcesLocal: sources.Count > 0 && local.Count == sources.Count,
            PricedAtShops: shops.Count > 0 || shopCalls.Count > 0,
            ShopToolCalls: shopCalls.Count,
            DealToolCalls: shopCalls.Count(c => Shops.DealTools.Contains(c.Name)),
            DealsNamed: Shops.DealsNamed(first.Answer),
            UntooledShops: Shops.UntooledShops(first.Answer),
            NoCatalogueLine: Shops.NoCatalogueLine(first.Answer),
            StoreForArea: Shops.StoreForArea(results),
            StoreDefault: Shops.StoreDefault(results),
            Ingredients: items,
            Priced: priced,
            Unsourced: guessed,
            PerKg: perKg,
            NoShopRows: noShopRows,
            WrongStore: wrongStore,
            // Right rows over the list's: priced with their source, or rightly without a price where there's no shop.
            PricedRatio: items is > 0 ? Math.Round((double)(priced!.Value + noShopRows) / items.Value, 2, MidpointRounding.ToEven) : null,
            RandAmounts: turns.Sum(t => Prices.RandAmounts(t.Answer)),
            UnconfirmedMarks: turns.Sum(t => Prices.UnconfirmedMarks(t.Answer)),
            Errors: turns.Count(t => t.Error is not null),
            FollowUp: followUp,
            FollowUpHandle: follow?.Continued);
    }
}
