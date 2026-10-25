using System.Text.RegularExpressions;

namespace LouisAgent.Evals.Puls.Shared.Scoring;

/// <summary>
/// The links in an answer, and whether a price cell is sourced by one: a price counts only with the link it came from, to the
/// shop whose column it is in (or, under a column that names none, to a shop) (F31 step 11b, review of PR #55).
/// </summary>
public static partial class Links
{
    /// <summary>A reference-style link's definition: `[label]: https://…`, and its label and URL.</summary>
    [GeneratedRegex(@"^\s*\[([^\]]+)\]:\s*(https?://\S+)", RegexOptions.Multiline)]
    private static partial Regex Reference();

    /// <summary>A reference-style link in a cell: `[text][label]`.</summary>
    [GeneratedRegex(@"\[([^\]]*)\]\[([^\]]*)\]")]
    private static partial Regex ReferenceLink();

    /// <summary>An inline link: `](https://…)`. Its URL is group 1.</summary>
    [GeneratedRegex(@"\]\((https?://[^)\s]+)")]
    public static partial Regex InlineLink();

    /// <summary>A price per kg, in its words (the link is taken out first, as a shop's address may hold /kg).</summary>
    [GeneratedRegex(@"(/\s*kg\b|\bper\s*(kg|kilo))", RegexOptions.IgnoreCase)]
    private static partial Regex PerKgWords();

    /// <summary>A pack sold by weight costs a range: `R87,99–R103,99`.</summary>
    [GeneratedRegex(@"\bR\s?\d[\d\s,.]*?\s*[–-]\s*R?\s?\d")]
    private static partial Regex Range();

    /// <summary>A product sold by weight: its link's path ends in its unit, KG (`…-10732975KG`).</summary>
    [GeneratedRegex(@"-\d+KG/?$")]
    private static partial Regex ByWeightPath();

    /// <summary>The answer's reference-style link definitions, by their label in lower case; a later one for a label wins.</summary>
    public static Dictionary<string, string> References(string answer)
    {
        var references = new Dictionary<string, string>();
        foreach (Match match in Reference().Matches(answer))
            references[match.Groups[1].Value.Trim().ToLowerInvariant()] = match.Groups[2].Value;
        return references;
    }

    /// <summary>The URLs a cell links to: inline ones, and reference-style ones by their label.</summary>
    public static List<string> Urls(string cell, IReadOnlyDictionary<string, string> references)
    {
        var urls = InlineLink().Matches(cell).Select(m => m.Groups[1].Value).ToList();
        foreach (Match link in ReferenceLink().Matches(cell))
        {
            string text = link.Groups[1].Value, label = link.Groups[2].Value;
            string key = (label.Length > 0 ? label : text).Trim().ToLowerInvariant();
            urls.Add(references.GetValueOrDefault(key, ""));
        }
        return urls.Where(url => url.Length > 0).ToList();
    }

    /// <summary>Whether a price is a price per kg: it says so, or it's one amount for a product sold by weight.</summary>
    public static bool PerKg(string cell, IReadOnlyDictionary<string, string> references)
    {
        if (PerKgWords().IsMatch(InlineLink().Replace(cell, "]"))) return true;
        return !Range().IsMatch(cell) && Urls(cell, references).Any(url => ByWeightPath().IsMatch(PathOf(url)));
    }

    /// <summary>Whether a price cell links to its source: the column's shop, or any shop when the column names none.</summary>
    public static bool Sourced(string cell, string? shop, IReadOnlyDictionary<string, string> references)
    {
        IEnumerable<string> sites = shop is not null ? [shop] : Shops.Hosts.Select(h => h.Host);
        return Urls(cell, references).Any(url => sites.Any(site => IsHost(url, site)));
    }

    /// <summary>Whether a URL is on the site or one of its subdomains (shop.woolworths.co.za is Woolworths; notcheckers isn't).</summary>
    internal static bool IsHost(string url, string site)
    {
        string host = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host.ToLowerInvariant() : "";
        return host == site || host.EndsWith("." + site, StringComparison.Ordinal);
    }

    private static string PathOf(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.AbsolutePath : "";
}
