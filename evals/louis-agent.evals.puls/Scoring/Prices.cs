using System.Text.RegularExpressions;

namespace LouisAgent.Evals.Puls.Scoring;

/// <summary>What a list's prices say: how many ingredients were priced with their source, how many were listed, how many were
/// guesses, and how many were a price per kg (neither priced nor guessed).</summary>
public sealed record PriceCount(int Priced, int Listed, int Guessed, int PerKg);

/// <summary>Where a basket's total was read from: the line that says it, or the sum of the priced rows.</summary>
public enum TotalSource
{
    TotalLine,
    RowSum,
}

/// <summary>A basket's total in rand, and the reading it came from.</summary>
public sealed record BasketTotal(decimal Amount, TotalSource Source);

/// <summary>
/// The price rules of the shopping list (F31 step 11b, review of PR #55 and #58, F34). A rand amount without its link is a
/// guess from memory, counted apart and never as priced (Louis: "the idea is to get an actual answer").
/// </summary>
public static partial class Prices
{
    [GeneratedRegex(@"\bR\s?\d[\d\s,.]*")]
    private static partial Regex Rand();

    [GeneratedRegex(@"\b(unknown|onbekend|geskat|skatting|skattings|estimate[ds]?|nie bevestig|unverified|onbevestig|nie gevind)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Estimate();

    [GeneratedRegex(@"\b(unknown|onbekend|geskat|skatting|skattings|estimate[ds]?|nie bevestig|unverified|onbevestig|snippets?|soekresultate|soekindeks)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Unconfirmed();

    /// <summary>How many rand amounts an answer gives.</summary>
    public static int RandAmounts(string? answer) => Rand().Matches(answer ?? "").Count;

    /// <summary>How often an answer says a value is unknown, estimated or from the search index.</summary>
    public static int UnconfirmedMarks(string? answer) => Unconfirmed().Matches(answer ?? "").Count;

    /// <summary>
    /// A list's ingredients as priced, listed, guessed and per kg; null when the answer has no list. A row is priced only with the
    /// link its price came from, to the shop whose column the price is in (or to a shop, under a column that names none); a
    /// reference-style link is its URL. A price per kg counts apart. A price whose row links its product at that price's shop
    /// is sourced by that link (F34). An estimate mark or ± is neither priced nor guessed.
    /// </summary>
    public static PriceCount? Priced(string? answer)
    {
        answer ??= "";
        var references = Links.References(answer);
        int listed = 0, priced = 0, guessed = 0, perKg = 0;
        foreach (var list in Lists.Of(answer))
        {
            foreach (var cells in list.Rows)
            {
                listed++;
                string row = "| " + string.Join(" | ", cells) + " |";
                string linked = string.Join(" ", cells.SelectMany(cell => Links.Urls(cell, references)).Select(url => $"[]({url})"));
                var sourced = new List<string>();
                for (int i = 1; i < cells.Count; i++)
                {
                    if (!Rand().IsMatch(cells[i])) continue;
                    string? shop = i < list.Header.Count ? Shops.ShopOf(list.Header[i]) : null;
                    bool ownLink = Links.Sourced(cells[i], shop, references);
                    if (ownLink || Links.Sourced(linked, shop, references))
                        sourced.Add(ownLink ? cells[i] : cells[i] + " " + linked);
                }

                if (sourced.Any(cell => !Links.PerKg(cell, references))) priced++;
                else if (sourced.Count > 0) perKg++;
                else if (Rand().IsMatch(row) && !Estimate().IsMatch(row) && !row.Contains('±')) guessed++;
            }
        }
        return listed == 0 ? null : new PriceCount(priced, listed, guessed, perKg);
    }

    [GeneratedRegex(@"\b(totaal|totale|total|altesaam)\b", RegexOptions.IgnoreCase)]
    private static partial Regex TotalWord();

    /// <summary>A line about what is saved, not what is spent: it may say "total" and give an amount that is not the basket's.</summary>
    [GeneratedRegex(@"\b(bespaar|besparing|spaar|sav(?:e|es|ed|ing|ings))\b", RegexOptions.IgnoreCase)]
    private static partial Regex Saving();

    /// <summary>
    /// The basket's total in rand (F18 step 5.4): the line that says the total, else the sum of the priced rows; null when the answer gives no
    /// amount it can read without guessing. Several total lines that disagree, or one that names two amounts, are no total; so is a priced row with two
    /// shops' prices (which is the basket's?) or a range. A row with a price but no link is a guess and a price per kg is not a pack's, so neither is summed.
    /// </summary>
    public static BasketTotal? Total(string? answer)
    {
        answer ??= "";
        var lines = answer.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);
        var amounts = lines.Where(line => TotalWord().IsMatch(line) && !Saving().IsMatch(line)).SelectMany(Money.Amounts).Distinct().ToList();
        if (amounts.Count == 1) return new BasketTotal(amounts[0], TotalSource.TotalLine);
        return amounts.Count > 1 ? null : RowSum(answer);
    }

    private static BasketTotal? RowSum(string answer)
    {
        var references = Links.References(answer);
        decimal sum = 0;
        int rows = 0;
        foreach (var list in Lists.Of(answer))
            foreach (var cells in list.Rows)
            {
                string linked = string.Join(" ", cells.SelectMany(cell => Links.Urls(cell, references)).Select(url => $"[]({url})"));
                var sourced = new List<string>();
                for (int i = 1; i < cells.Count; i++)
                {
                    if (Money.Amounts(cells[i]).Count == 0) continue;
                    string? shop = i < list.Header.Count ? Shops.ShopOf(list.Header[i]) : null;
                    if (Links.Sourced(cells[i], shop, references) || Links.Sourced(linked, shop, references)) sourced.Add(cells[i]);
                }

                if (sourced.Count == 0 || sourced.Any(cell => Links.PerKg(cell, references))) continue;
                var amounts = Money.Amounts(sourced[0]);
                if (sourced.Count > 1 || amounts.Count != 1) return null;
                sum += amounts[0];
                rows++;
            }
        return rows == 0 ? null : new BasketTotal(sum, TotalSource.RowSum);
    }

    /// <summary>The shop sites a list's prices link to, in the order they first appear: where the prices say they came from.</summary>
    public static IReadOnlyList<string> PriceLinkHosts(string? answer)
    {
        answer ??= "";
        var references = Links.References(answer);
        var sites = new List<string>();
        foreach (var cell in Lists.Of(answer).SelectMany(list => list.Rows).SelectMany(cells => cells.Skip(1)).Where(cell => Money.Amounts(cell).Count > 0))
            foreach (string url in Links.Urls(cell, references))
                if (Shops.SiteOf(url) is { } site && !sites.Contains(site)) sites.Add(site);
        return sites;
    }
}
