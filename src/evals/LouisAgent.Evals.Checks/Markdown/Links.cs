using System.Text.RegularExpressions;

namespace LouisAgent.Evals.Markdown;

/// <summary>
/// The links in an answer: inline ones, reference-style ones by their label, the images it shows, and whether a link
/// is on a site.
/// </summary>
/// <remarks>
/// In the check contract (Louis's pick, F49.3 step 1a), so an identity, a pack and the harness read a link the same
/// way (Move Method, Fowler). What only Puls knows about a link (a price per kg, a price's source, a recipe page's
/// share buttons) stayed in Puls, in <c>PriceLinks</c> and <c>RecipeLinks</c> (Extract Class).
/// </remarks>
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

    /// <summary>
    /// The answer's reference-style link definitions, by their label in lower case; a later one for a label wins.
    /// </summary>
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

    /// <summary>A markdown image, <c>![alt](url)</c>. Its URL is group 1.</summary>
    [GeneratedRegex(@"!\[[^\]]*\]\((https?://[^)\s]+)\)")]
    private static partial Regex Image();

    /// <summary>
    /// The images a text shows, as <c>![alt](url)</c> (also inside a link to a page), in order; a plain link is no
    /// image.
    /// </summary>
    public static IReadOnlyList<string> ImageUrls(string? text) =>
        Image().Matches(text ?? "").Select(m => m.Groups[1].Value).ToList();

    /// <summary>
    /// Whether a URL is on the site or one of its subdomains (shop.woolworths.co.za is Woolworths; notcheckers isn't).
    /// </summary>
    public static bool IsHost(string url, string site)
    {
        string host = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host.ToLowerInvariant() : "";
        return host == site || host.EndsWith("." + site, StringComparison.Ordinal);
    }
}
