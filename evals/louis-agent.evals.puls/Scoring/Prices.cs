using System.Text.RegularExpressions;

namespace LouisAgent.Evals.Puls.Scoring;

/// <summary>What a list's prices say: how many ingredients were priced with their source, how many were listed, how many were
/// guesses, and how many were a price per kg (neither priced nor guessed).</summary>
public sealed record PriceCount(int Priced, int Listed, int Guessed, int PerKg);

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
}
