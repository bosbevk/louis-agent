using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LouisAgent.Packs.Recipes.Reading;

/// <summary>
/// Finds a page's lead image and hands it back as <c>![title](url)</c> (FX24). It looks where the page declares its
/// photo for machines, not where the body shows it, because a page's photo is often a lazy-loaded image outside
/// <c>main</c> or one with no alt text, which the page text keeps none of. Only an http(s) file that isn't an SVG
/// counts, so a data URI or a logo is no lead image.
/// </summary>
/// <remarks>
/// Chain of fallbacks. The page's JSON-LD (the <c>image</c> of its main item, read through
/// <see cref="JsonLdBlocks"/>) is tried first because it names the photo of the page's subject; the <c>og:image</c>
/// meta tag comes second because every page that shares a link has one. Without the chain a page that declares only
/// one of them would show no photo.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0027-engine-identities-and-packs.md">ADR 0027: engine, identities and packs</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: regex is the last option</seealso>
internal static partial class LeadImage
{
    /// <summary>
    /// Gives you the page's lead image as markdown, or nothing when the page declares none the text can show.
    /// </summary>
    internal static string? Of(string html, Uri baseUri, string? title)
    {
        string? file = MainEntityImage(html) ?? OpenGraphImage(html);
        if (file is null || !Uri.TryCreate(baseUri, file.Trim(), out var target)
                         || target.Scheme is not ("http" or "https") ||
                         target.AbsolutePath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            return null;

        string alt = (title ?? "").Replace('[', '(').Replace(']', ')');
        return $"![{(alt.Length > 0 ? alt : "image")}]({target})";
    }

    /// <summary>
    /// Finds the first image of whatever the page is about, wherever its JSON-LD puts it (the document, an array, a
    /// <c>@graph</c>). Core names no type of subject (ADR 0027), so any is taken, but the entities that describe the
    /// site and not the page are skipped (<see cref="SiteEntities"/>).
    /// </summary>
    private static string? MainEntityImage(string html)
    {
        foreach (var document in JsonLdBlocks.In(html))
        {
            // The documents are ours to dispose; the first block that names an image wins.
            using (document)
            {
                if (ImageOfEntity(document.RootElement) is { } file) return file;
            }
        }

        return null;
    }

    /// <summary>
    /// Looks through a JSON-LD node (an array, or an object and its <c>@graph</c>) for its subject's image.
    /// </summary>
    private static string? ImageOfEntity(JsonElement node)
    {
        switch (node.ValueKind)
        {
            case JsonValueKind.Array:
                return node.EnumerateArray().Select(ImageOfEntity).FirstOrDefault(file => file is not null);
            case JsonValueKind.Object:
                if (node.TryGetProperty("@graph", out var graph) && ImageOfEntity(graph) is { } inGraph) return inGraph;
                return IsSubject(node) && node.TryGetProperty("image", out var image) ? FirstFile(image) : null;
            default:
                return null;
        }
    }

    /// <summary>
    /// The kinds of entity that describe the site or the page's furniture, never its subject: their image is a logo,
    /// an author's face or the page's own.
    /// </summary>
    private static readonly HashSet<string> SiteEntities =
        new(["Organization", "WebSite", "WebPage", "Person", "ImageObject", "BreadcrumbList"], StringComparer.Ordinal);

    /// <summary>
    /// Whether an entity is what the page is about: it has a type, and not only site types
    /// (<c>["Article","WebPage"]</c> is an article).
    /// </summary>
    private static bool IsSubject(JsonElement node)
    {
        if (!node.TryGetProperty("@type", out var type)) return false;

        var types = type.ValueKind switch
        {
            JsonValueKind.String => [type.GetString()!],
            JsonValueKind.Array => type.EnumerateArray().Where(t => t.ValueKind == JsonValueKind.String)
                .Select(t => t.GetString()!).ToList(),
            _ => new List<string>(),
        };
        return types.Any(t => !SiteEntities.Contains(t));
    }

    /// <summary>
    /// An <c>image</c> is a URL, an ImageObject with a <c>url</c>, or a list of either: this takes the first one that
    /// names a file.
    /// </summary>
    private static string? FirstFile(JsonElement image) => image.ValueKind switch
    {
        JsonValueKind.String => image.GetString() is { Length: > 0 } text ? WebUtility.HtmlDecode(text) : null,
        JsonValueKind.Object => image.TryGetProperty("url", out var url) ? FirstFile(url) : null,
        JsonValueKind.Array => image.EnumerateArray().Select(FirstFile).FirstOrDefault(file => file is not null),
        _ => null,
    };

    /// <summary>
    /// Finds the <c>content</c> of the page's <c>og:image</c> (or <c>twitter:image</c>) meta tag, whichever order its
    /// attributes are in.
    /// </summary>
    private static string? OpenGraphImage(string html)
    {
        foreach (Match tag in Meta().Matches(html))
        {
            var attributes = Attribute().Matches(tag.Value).ToDictionary(a => a.Groups["name"].Value.ToLowerInvariant(),
                a => WebUtility.HtmlDecode(a.Groups["value"].Value), StringComparer.Ordinal);
            string? name = attributes.GetValueOrDefault("property") ?? attributes.GetValueOrDefault("name");
            if (name is "og:image" or "og:image:secure_url" or "twitter:image" &&
                attributes.GetValueOrDefault("content") is { Length: > 0 } content)
                return content;
        }

        return null;
    }

    /// <summary>
    /// Matches one <c>&lt;meta ...&gt;</c> tag. Matches <c>&lt;meta property="og:image" content="a.jpg"&gt;</c>; does
    /// not match <c>&lt;metadata&gt;</c>.
    /// </summary>
    [GeneratedRegex(@"<meta\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex Meta();

    /// <summary>
    /// Matches one quoted attribute and splits it into <c>name</c> and <c>value</c>. Matches <c>content="a.jpg"</c>
    /// and <c>content='a.jpg'</c>; does not match <c>content=a.jpg</c>.
    /// </summary>
    [GeneratedRegex(@"(?<name>[\w:-]+)\s*=\s*(?:""(?<value>[^""]*)""|'(?<value>[^']*)')")]
    private static partial Regex Attribute();
}
