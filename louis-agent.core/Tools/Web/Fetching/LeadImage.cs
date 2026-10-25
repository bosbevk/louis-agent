namespace LouisAgent.Core.Tools.Web.Fetching;

using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>
/// A page's lead image as <c>![title](url)</c> (FX24): the photo of the dish on a recipe page, found where a page declares it, not where its body
/// shows it. A recipe page's photo is often a lazy-loaded image outside <c>main</c>, or one with no alt text, which the page text keeps none of; the
/// page's JSON-LD <c>Recipe.image</c> (the recipe's own, first) or its <c>og:image</c> names it for machines. Only an http(s) file that isn't an SVG
/// counts, so a data URI or a logo is no lead image.
/// </summary>
internal static partial class LeadImage
{
    /// <summary>The lead image's markdown, or null when the page declares none the text can show.</summary>
    internal static string? Of(string html, Uri baseUri, string? title)
    {
        string? file = RecipeImage(html) ?? OpenGraphImage(html);
        if (file is null || !Uri.TryCreate(baseUri, file.Trim(), out var target)
            || target.Scheme is not ("http" or "https") || target.AbsolutePath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            return null;
        string alt = (title ?? "").Replace('[', '(').Replace(']', ')');
        return $"![{(alt.Length > 0 ? alt : "image")}]({target})";
    }

    /// <summary>The first <c>image</c> of a JSON-LD object whose <c>@type</c> is Recipe, wherever it sits (the document, an array, a <c>@graph</c>).</summary>
    private static string? RecipeImage(string html)
    {
        foreach (Match block in JsonLd().Matches(html))
        {
            try
            {
                using var document = JsonDocument.Parse(WebUtility.HtmlDecode(block.Groups[1].Value));
                if (ImageOfRecipe(document.RootElement) is { } file) return file;
            }
            catch (JsonException)
            {
                // A script the page left malformed says nothing; the next block, or the og:image, may.
            }
        }
        return null;
    }

    private static string? ImageOfRecipe(JsonElement node)
    {
        switch (node.ValueKind)
        {
            case JsonValueKind.Array:
                return node.EnumerateArray().Select(ImageOfRecipe).FirstOrDefault(file => file is not null);
            case JsonValueKind.Object:
                if (node.TryGetProperty("@graph", out var graph) && ImageOfRecipe(graph) is { } inGraph) return inGraph;
                return IsRecipe(node) && node.TryGetProperty("image", out var image) ? FirstFile(image) : null;
            default:
                return null;
        }
    }

    private static bool IsRecipe(JsonElement node) =>
        node.TryGetProperty("@type", out var type) && type.ValueKind switch
        {
            JsonValueKind.String => type.GetString() == "Recipe",
            JsonValueKind.Array => type.EnumerateArray().Any(t => t.ValueKind == JsonValueKind.String && t.GetString() == "Recipe"),
            _ => false,
        };

    /// <summary>An <c>image</c> is a URL, an ImageObject with a <c>url</c>, or a list of either: the first one that names a file.</summary>
    private static string? FirstFile(JsonElement image) => image.ValueKind switch
    {
        JsonValueKind.String => image.GetString() is { Length: > 0 } text ? text : null,
        JsonValueKind.Object => image.TryGetProperty("url", out var url) ? FirstFile(url) : null,
        JsonValueKind.Array => image.EnumerateArray().Select(FirstFile).FirstOrDefault(file => file is not null),
        _ => null,
    };

    /// <summary>The <c>content</c> of the page's <c>og:image</c> (or <c>twitter:image</c>) meta tag, whichever order its attributes are in.</summary>
    private static string? OpenGraphImage(string html)
    {
        foreach (Match tag in Meta().Matches(html))
        {
            var attributes = Attribute().Matches(tag.Value).ToDictionary(a => a.Groups["name"].Value.ToLowerInvariant(),
                a => WebUtility.HtmlDecode(a.Groups["value"].Value), StringComparer.Ordinal);
            string? name = attributes.GetValueOrDefault("property") ?? attributes.GetValueOrDefault("name");
            if (name is "og:image" or "og:image:secure_url" or "twitter:image" && attributes.GetValueOrDefault("content") is { Length: > 0 } content)
                return content;
        }
        return null;
    }

    [GeneratedRegex(@"<script\b[^>]*type\s*=\s*[""']application/ld\+json[""'][^>]*>(.*?)</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex JsonLd();

    [GeneratedRegex(@"<meta\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex Meta();

    [GeneratedRegex(@"(?<name>[\w:-]+)\s*=\s*(?:""(?<value>[^""]*)""|'(?<value>[^']*)')")]
    private static partial Regex Attribute();
}
