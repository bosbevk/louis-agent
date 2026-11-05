using System.Text.RegularExpressions;

namespace LouisAgent.Packs.Recipes.Reading;

/// <summary>
/// Finds where a page's photo of the dish might be, best guess first, and takes the other images out of its text. The
/// pages we read get this wrong in every way: Zola Nene declares no photo and shows it with no description, The Food
/// Fox declares a dead server, and Vrouekeur's text is full of other recipes' pictures.
/// </summary>
/// <remarks>
/// Chain of Responsibility, one link per place to look: what the page declares for machines first, because it names the
/// page's subject, then the first real image after the title, because that's where a recipe page shows its dish.
/// <see cref="PhotoCheck"/> walks the chain and stops at the first that loads. Adding a place to look is one more link.
/// An image server's copy comes after the site's own, since its copy can be gone while the site's is fine. Reading the
/// tags is plain string work, with one regex for the images in the text (ADR 0057).
/// </remarks>
/// <seealso href="https://refactoring.guru/design-patterns/chain-of-responsibility">Chain of Responsibility</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0080-a-tool-when-it-improves-the-prompt.md">ADR 0080: a tool when it improves the prompt</seealso>
internal static partial class PagePhotos
{
    /// <summary>Words in an address that mean page furniture, not a photo of the dish.</summary>
    private static readonly string[] Furniture =
    [
        "logo", "icon", "avatar", "gravatar", "sprite", "banner", "header", "pixel", "emoji", "spacer", "placeholder",
        "/plugins/", "/themes/", ".svg"
    ];

    /// <summary>Where to read an image's file, best first: a <c>srcset</c> holds the site's real copy.</summary>
    private static readonly string[] ImageAttributes = ["srcset", "data-srcset", "data-src", "data-lazy-src", "src"];

    /// <summary>The most addresses one page may cost us to check.</summary>
    private const int MostToCheck = 4;

    /// <summary>The page's possible photos, best first; <paramref name="declared"/> is its recipe data's.</summary>
    public static IEnumerable<Uri> Candidates(string html, Uri page, Uri? declared) =>
        new[] { declared ?? Declared(html, page), FirstAfterTitle(html, page) }.OfType<Uri>()
            .Where(IsPhoto).SelectMany(WithTheSitesCopy).Distinct().Take(MostToCheck);

    /// <summary>A page's text with its images gone, and no gaps left where they were.</summary>
    public static string WithoutImages(string text)
    {
        var lines = ImageInText().Replace(text, "").Split('\n').Select(line => line.Trim());
        return ManyBlankLines().Replace(string.Join('\n', lines), "\n\n").Trim();
    }

    /// <summary>What the page's tags and data declare, as an address.</summary>
    private static Uri? Declared(string html, Uri page)
    {
        if (LeadImage.Of(html, page, null) is not { } lead) return null;

        int start = lead.LastIndexOf("](", StringComparison.Ordinal) + 2;
        return Uri.TryCreate(lead[start..^1], UriKind.Absolute, out var file) ? file : null;
    }

    /// <summary>The first real image after the page's title; a page with no title is read from the top.</summary>
    private static Uri? FirstAfterTitle(string html, Uri page)
    {
        int at = Math.Max(0, html.IndexOf("<h1", StringComparison.OrdinalIgnoreCase));
        while ((at = html.IndexOf("<img", at, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            int end = html.IndexOf('>', at);
            if (end < 0) return null;

            if (FileOf(html[at..end], page) is { } file && IsPhoto(file)) return file;
            at = end;
        }

        return null;
    }

    /// <summary>An image tag's file: the first web address among its attributes, in the order above.</summary>
    private static Uri? FileOf(string tag, Uri page) =>
        ImageAttributes.Select(name => Attribute(tag, name)).OfType<string>()
            .Select(value => value.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault())
            .Select(value => Uri.TryCreate(page, value, out var file) ? file : null)
            .FirstOrDefault(file => file?.Scheme is "http" or "https");

    /// <summary>One attribute's value, in either kind of quote.</summary>
    private static string? Attribute(string tag, string name)
    {
        foreach (char quote in "\"'")
        {
            string opening = $" {name}={quote}";
            int start = tag.IndexOf(opening, StringComparison.OrdinalIgnoreCase);
            if (start < 0) start = tag.IndexOf($"\n{name}={quote}", StringComparison.OrdinalIgnoreCase);
            if (start < 0) continue;

            start += opening.Length;
            int end = tag.IndexOf(quote, start);
            if (end > start) return tag[start..end];
        }

        return null;
    }

    /// <summary>Whether an address could be a photo of the dish.</summary>
    private static bool IsPhoto(Uri file) =>
        !Furniture.Any(word => file.AbsoluteUri.Contains(word, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// An address, after the site's own copy when it's on WordPress's image servers:
    /// <c>https://i0.wp.com/www.site.co.za/up/pie.jpg?w=1080</c> is <c>https://www.site.co.za/up/pie.jpg</c>.
    /// </summary>
    private static IEnumerable<Uri> WithTheSitesCopy(Uri file)
    {
        if (file.Host.EndsWith(".wp.com", StringComparison.OrdinalIgnoreCase) &&
            Uri.TryCreate($"https:/{file.AbsolutePath}", UriKind.Absolute, out var own))
            yield return own;

        yield return file;
    }

    /// <summary>
    /// An image in a page's text, with the link around it if it has one: matches <c>![a](b)</c> and
    /// <c>[![a](b)](c)</c>, not <c>[a](b)</c>.
    /// </summary>
    [GeneratedRegex(@"\[?!\[[^\]]*\]\([^)\s]*\)(?:\]\([^)\s]*\))?", RegexOptions.NonBacktracking)]
    private static partial Regex ImageInText();

    /// <summary>Three line ends or more: matches a gap left by an image, not a paragraph break.</summary>
    [GeneratedRegex(@"\n{3,}", RegexOptions.NonBacktracking)]
    private static partial Regex ManyBlankLines();
}
