using LouisAgent.Evals.Puls.Expectations;
using LouisAgent.Evals.Puls.Scoring;
using LouisAgent.Evals.Trials;
using LouisAgent.Evals.Puls.Turns;

namespace LouisAgent.Evals.Puls.Cook;

/// <summary>
/// The cook check's score function (the Python's <c>cook/run_check.py</c> <c>score</c>): one conversation's turns as numbers.
/// A meal goes to the cook when the mode has it and the scenario's `routes_to` says so; otherwise, and for the control question, Puls answers itself. An errored
/// first turn is never right. The no-shop rule is the one-key rule (step 3.3), the one place the C# differs from the Python.
/// </summary>
public static class CookScorer
{
    /// <summary>The shops' sites, sorted and without repeats (the Python's SHOPS).</summary>
    private static readonly string[] SiteHosts = Shops.Hosts.Select(h => h.Host).Distinct().OrderBy(h => h, StringComparer.Ordinal).ToArray();

    /// <summary>Whether a mode has the cook: the Python's rule, the specialists toolset (cook/run_check.py), whatever the mode is named.</summary>
    public static bool CookOn(EvalMode mode) => mode.Toolsets.Contains("specialists", StringComparer.Ordinal);

    /// <summary>One trial as the cook's scores: the grader's one helper, so every cook grader scores a trial the same way.</summary>
    public static CookScores ScoreTrial(Trial trial) =>
        Score(CookExpectations.Of(trial.Scenario), trial.Transcript.Turns.Select(TurnView.From).ToList(), CookOn(trial.Mode));

    /// <summary>
    /// One conversation as the Python's keys. What it should have done is the scenario's <paramref name="expected"/> (F18 step 5.4): the route
    /// when the mode has the cook, and whether the town has a shop, not a rule on the scenario's name.
    /// </summary>
    public static CookScores Score(CookExpectations expected, IReadOnlyList<TurnView> turns, bool cookOn)
    {
        var first = turns[0];
        var follow = turns.Count > 1 ? turns[1] : null;

        var price = Prices.Priced(first.Answer);
        int? items = price?.Listed, priced = price?.Priced, guessed = price?.Guessed, perKg = price?.PerKg;
        int noShopRows = 0, wrongStore = 0;
        if (expected.Shop == ShopExpectation.Any && items is > 0)
        {
            // One key (F18 step 3.3, TODO line 30): in a town with no shop, a priced row is wrong_store with or without its
            // link, and so is an unlinked price; unsourced counts only where the town has a shop.
            wrongStore = (priced ?? 0) + (guessed ?? 0);
            priced = 0;
            guessed = 0;
            noShopRows = items.Value - wrongStore - (perKg ?? 0);
        }

        var allCalls = turns.SelectMany(t => t.Calls.Concat(t.SpecialistCalls)).ToList();
        var fetched = ToolHosts.Of(allCalls);
        var sources = fetched.Where(h => !Shops.MapHosts.Any(map => h.EndsWith(map, StringComparison.Ordinal))).ToList();
        var local = sources.Where(h => h.EndsWith(".za", StringComparison.Ordinal)).ToList();
        var shops = fetched.Where(h => SiteHosts.Any(site => h.EndsWith(site, StringComparison.Ordinal))).ToList();
        string? expectedRole = cookOn ? expected.RoutesTo : null;
        var shopCalls = allCalls.Where(c => Shops.ShopTools.Contains(c.Name)).ToList();
        var results = turns.SelectMany(t => t.Results.Concat(t.SpecialistResults)).ToList();

        string? followUp = follow is null ? null
            : follow.Continued is not null ? "continued"
            : follow.Role is not null ? "new sub-session"
            : "answered itself";

        return new CookScores(
            Role: first.Role,
            RoutedRight: first.Error is null && first.Role == expectedRole,
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
