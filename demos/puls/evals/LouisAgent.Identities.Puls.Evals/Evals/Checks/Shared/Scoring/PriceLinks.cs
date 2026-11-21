using System.Text.RegularExpressions;
using LouisAgent.Evals.Markdown;

namespace LouisAgent.Identities.Puls.Evals.Checks.Shared.Scoring;

/// <summary>
/// What a price cell's links say about the price: whether it's a price per kg, and whether it's sourced by a link to
/// the shop whose column it is in (or, under a column that names none, to any shop).
/// </summary>
/// <remarks>
/// Puls's own part of the old <c>Links</c> (Extract Class, Fowler, F49.3 step 1a): the plain reading of a link went
/// to the check contract's <see cref="Links"/>; how the cook writes a price, and which sites are shops, are Puls's.
/// </remarks>
public static partial class PriceLinks
{
    /// <summary>A price per kg, in its words (the link is taken out first, as a shop's address may hold /kg).</summary>
    [GeneratedRegex(@"(/\s*kg\b|\bper\s*(kg|kilo))", RegexOptions.IgnoreCase)]
    private static partial Regex PerKgWords();

    /// <summary>A pack sold by weight costs a range: `R87,99–R103,99`.</summary>
    [GeneratedRegex(@"\bR\s?\d[\d\s,.]*?\s*[–-]\s*R?\s?\d")]
    private static partial Regex Range();

    /// <summary>A product sold by weight: its link's path ends in its unit, KG (`…-10732975KG`).</summary>
    [GeneratedRegex(@"-\d+KG/?$")]
    private static partial Regex ByWeightPath();

    /// <summary>
    /// Whether a price is a price per kg: it says so, or it's one amount for a product sold by weight.
    /// </summary>
    public static bool PerKg(string cell, IReadOnlyDictionary<string, string> references)
    {
        if (PerKgWords().IsMatch(Links.InlineLink().Replace(cell, "]"))) return true;
        return !Range().IsMatch(cell) && Links.Urls(cell, references).Any(url => ByWeightPath().IsMatch(PathOf(url)));
    }

    /// <summary>
    /// Whether a price cell links to its source: the column's shop, or any shop when the column names none.
    /// </summary>
    public static bool Sourced(string cell, string? shop, IReadOnlyDictionary<string, string> references)
    {
        IEnumerable<string> sites = shop is not null ? [shop] : Shops.Hosts.Select(h => h.Host);
        return Links.Urls(cell, references).Any(url => sites.Any(site => Links.IsHost(url, site)));
    }

    /// <summary>A URL's path, or nothing if it isn't a full address.</summary>
    private static string PathOf(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.AbsolutePath : "";
}
