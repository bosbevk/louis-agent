using System.Text.RegularExpressions;
using LouisAgent.Evals.Markdown;
using LouisAgent.Identities.Puls.Evals.Checks.Shared.Scoring;

namespace LouisAgent.Identities.Puls.Evals.Checks.Cook.Scoring;

/// <summary>
/// What a list's prices say: how many ingredients were listed, how many priced with their source, how many guessed,
/// how many a price per kg (neither priced nor guessed), and how many estimates.
/// </summary>
/// <remarks>
/// An estimate is a rand amount marked ± or called an estimate: the cook's answer while the shops' prices are awaited.
/// </remarks>
public sealed record PriceCount(int Priced, int Listed, int Guessed, int PerKg, int Estimated = 0);

/// <summary>Where a basket's total was read from: the line that says it, or the sum of the priced rows.</summary>
public enum TotalSource
{
    TotalLine,
    RowSum,
    ListTotalRow,
}

/// <summary>A basket's total in rand, and the reading it came from.</summary>
public sealed record BasketTotal(decimal Amount, TotalSource Source);

/// <summary>
/// The price rules of the shopping list: what counts as priced, guessed or estimated, and the basket's total.
/// </summary>
/// <remarks>
/// A rand amount without its link is a guess from memory, counted apart and never as priced (Louis: "the idea is to
/// get an actual answer").
/// </remarks>
public static partial class Prices
{
    /// <summary>Any rand amount, loosely, for counting. Matches "R149,95" and "R 1 249"; not "149".</summary>
    [GeneratedRegex(@"\bR\s?\d[\d\s,.]*")]
    private static partial Regex Rand();

    /// <summary>A word that marks a price as unsure. Matches "geskat"; not "gesien".</summary>
    [GeneratedRegex(
        @"\b(unknown|onbekend|geskat|skatting|skattings|estimate[ds]?|nie bevestig|unverified|onbevestig|nie gevind)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex Estimate();

    /// <summary>
    /// A word that says a value isn't confirmed, search snippets included. Matches "soekresultate"; not "resultate".
    /// </summary>
    [GeneratedRegex(
        @"\b(unknown|onbekend|geskat|skatting|skattings|estimate[ds]?|nie bevestig|unverified|onbevestig|snippets?|soekresultate|soekindeks)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex Unconfirmed();

    /// <summary>How many rand amounts an answer gives.</summary>
    public static int RandAmounts(string? answer) => Rand().Matches(answer ?? "").Count;

    /// <summary>How often an answer says a value is unknown, estimated or from the search index.</summary>
    public static int UnconfirmedMarks(string? answer) => Unconfirmed().Matches(answer ?? "").Count;

    /// <summary>
    /// Counts a list's ingredients as priced, listed, guessed, per kg and estimated; nothing when the answer has no
    /// list.
    /// </summary>
    /// <remarks>
    /// A row is priced only with the link its price came from, to the shop whose column the price is in (or to any
    /// shop, under a column that names none); a reference-style link counts as its URL. A price whose row links its
    /// product at that price's shop is sourced by that link. A price per kg counts apart, and an estimate mark or ± is
    /// neither priced nor guessed.
    /// </remarks>
    public static PriceCount? Priced(string? answer)
    {
        answer ??= "";
        var references = Links.References(answer);
        int listed = 0, priced = 0, guessed = 0, perKg = 0, estimated = 0;
        foreach (var list in Lists.Of(answer))
        {
            foreach (var cells in list.Rows)
            {
                listed++;
                string row = "| " + string.Join(" | ", cells) + " |";
                string linked = string.Join(" ",
                    cells.SelectMany(cell => Links.Urls(cell, references)).Select(url => $"[]({url})"));
                var sourced = new List<string>();
                for (int i = 1; i < cells.Count; i++)
                {
                    if (!Rand().IsMatch(cells[i])) continue;
                    string? shop = i < list.Header.Count ? Shops.ShopOf(list.Header[i]) : null;
                    bool ownLink = PriceLinks.Sourced(cells[i], shop, references);
                    if (ownLink || PriceLinks.Sourced(linked, shop, references))
                        sourced.Add(ownLink ? cells[i] : cells[i] + " " + linked);
                }

                if (sourced.Any(cell => !PriceLinks.PerKg(cell, references))) priced++;
                else if (sourced.Count > 0) perKg++;
                else if (Rand().IsMatch(row) && (Estimate().IsMatch(row) || row.Contains('±'))) estimated++;
                else if (Rand().IsMatch(row)) guessed++;
            }
        }

        return listed == 0 ? null : new PriceCount(priced, listed, guessed, perKg, estimated);
    }

    /// <summary>A word for the total. Matches "Totaal"; not "totally".</summary>
    [GeneratedRegex(@"\b(totaal|totale|total|altesaam)\b", RegexOptions.IgnoreCase)]
    private static partial Regex TotalWord();

    /// <summary>
    /// A line about what's saved, not spent: it may say "total" with an amount that isn't the basket's. Matches
    /// "bespaar"; not "spaarbank".
    /// </summary>
    [GeneratedRegex(@"\b(bespaar|besparing|spaar|sav(?:e|es|ed|ing|ings))\b", RegexOptions.IgnoreCase)]
    private static partial Regex Saving();

    /// <summary>
    /// The basket's total in rand: the list's total row, else the line that says the total, else the sum of the priced
    /// rows. Nothing when the answer gives no amount it can read without guessing.
    /// </summary>
    /// <remarks>
    /// Several total lines that disagree, or one that names two amounts, are no total; nor is a priced row with two
    /// shops' prices (which one is the basket's?) or a range. A row with a price but no link is a guess, and a price
    /// per kg isn't a pack's, so neither is summed.
    /// </remarks>
    public static BasketTotal? Total(string? answer)
    {
        answer ??= "";
        // The list's own total row is the basket's total: an answer says "total" elsewhere too, in a sentence or in the
        // unsure table, with the budget or a saving beside it, and those lines would disagree. Per person is a share,
        // not a total. Two total rows that disagree are no total.
        var listTotals = Lists.TotalRows(answer).Where(cells => TotalWord().IsMatch(cells[0]))
            .SelectMany(cells => cells.Skip(1)).SelectMany(Money.Amounts).Distinct().ToList();
        if (listTotals.Count == 1) return new BasketTotal(listTotals[0], TotalSource.ListTotalRow);
        if (listTotals.Count > 1) return null;

        var lines = answer.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);
        var amounts = lines.Where(line => TotalWord().IsMatch(line) && !Saving().IsMatch(line))
            .SelectMany(Money.Amounts).Distinct().ToList();
        if (amounts.Count == 1) return new BasketTotal(amounts[0], TotalSource.TotalLine);
        return amounts.Count > 1 ? null : RowSum(answer);
    }

    /// <summary>
    /// The sum of the rows priced with their source, one price each; nothing if a row has two shops' prices or a range.
    /// </summary>
    private static BasketTotal? RowSum(string answer)
    {
        var references = Links.References(answer);
        decimal sum = 0;
        int rows = 0;
        foreach (var list in Lists.Of(answer))
            foreach (var cells in list.Rows)
            {
                string linked = string.Join(" ",
                    cells.SelectMany(cell => Links.Urls(cell, references)).Select(url => $"[]({url})"));
                var sourced = new List<string>();
                for (int i = 1; i < cells.Count; i++)
                {
                    if (Money.Amounts(cells[i]).Count == 0) continue;
                    string? shop = i < list.Header.Count ? Shops.ShopOf(list.Header[i]) : null;
                    if (PriceLinks.Sourced(cells[i], shop, references) || PriceLinks.Sourced(linked, shop, references))
                        sourced.Add(cells[i]);
                }

                if (sourced.Count == 0 || sourced.Any(cell => PriceLinks.PerKg(cell, references))) continue;
                var amounts = Money.Amounts(sourced[0]);
                if (sourced.Count > 1 || amounts.Count != 1) return null;
                sum += amounts[0];
                rows++;
            }

        return rows == 0 ? null : new BasketTotal(sum, TotalSource.RowSum);
    }

    /// <summary>
    /// The shop sites a list's prices link to, in the order they first appear: where the prices say they came from.
    /// </summary>
    public static IReadOnlyList<string> PriceLinkHosts(string? answer)
    {
        answer ??= "";
        var references = Links.References(answer);
        var sites = new List<string>();
        foreach (var cell in Lists.Of(answer).SelectMany(list => list.Rows).SelectMany(cells => cells.Skip(1))
                     .Where(cell => Money.Amounts(cell).Count > 0))
            foreach (string url in Links.Urls(cell, references))
                if (Shops.SiteOf(url) is { } site && !sites.Contains(site))
                    sites.Add(site);
        return sites;
    }
}
