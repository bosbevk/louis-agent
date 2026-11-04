using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Shared.Text;

public static class Markdown
{
    // Raw HTML is shown as text, never rendered: answers can quote web pages or files, and that must not run as markup.
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    /// <summary>Parses with the pipeline the answers are rendered with, so <see cref="ImageLinks"/> reads "an image" the way the page draws it.</summary>
    internal static MarkdownDocument Parse(string markdown) => Markdig.Markdown.Parse(markdown, Pipeline);

    /// <param name="markdown">The text to show.</param>
    /// <param name="shown">The exact URLs of the images a tool gave in this turn (<c>ImageSources</c>, FX22); none by default.</param>
    public static MarkupString ToHtml(string markdown, IReadOnlySet<string>? shown = null)
    {
        MarkdownDocument document = Parse(markdown);

        // For the same reason, only plain web/mail links survive (no javascript: or data: URLs). A browser loads an image
        // by itself, so a model-written image could send the chat's data to any host. Two rules show one, and only these:
        // the URL is exactly one a tool returned in this turn (FX22: the recipe page's image; a model can't invent a URL
        // that passes), or its host is a shop's own image host (the cook's product photos, F33, kept so chats saved before
        // FX22 still show them). Any other image becomes a link, loaded only if the user clicks it.
        foreach (var link in document.Descendants<LinkInline>())
        {
            if (!IsSafeUrl(link.Url)) link.Url = "#";
            if (link.IsImage && !(IsToolImage(link.Url, shown) || IsShopImage(link.Url))) link.IsImage = false;
            // A shop's photo opens its original in a dialog when clicked (chat.js): the same file, without the size.
            if (link.IsImage) link.GetAttributes().AddProperty("data-original", WithoutSize(link.Url!));
        }

        foreach (var link in document.Descendants<AutolinkInline>())
        {
            if (!IsSafeUrl(link.Url)) link.Url = "#";
        }

        return new MarkupString(document.ToHtml(Pipeline));
    }

    /// <summary>
    /// The hosts whose images are shown: the shops' own image hosts, from the catalogue the cook reads (Checkers' cards
    /// load from here) and the shops' pages it renders (F33). An entry starting with a dot is a domain: Pick n Pay
    /// numbers its CDN hosts (cdn-prd-01, cdn-prd-02, ...), so any host under its domain counts.
    /// </summary>
    public static readonly IReadOnlyList<string> ImageHosts =
        ["catalog.sixty60.co.za", ".pnp.co.za", "assets.woolworthsstatic.co.za"];

    private static bool IsToolImage(string? url, IReadOnlySet<string>? shown) =>
        url is not null && shown is not null && shown.Contains(url);

    private static bool IsShopImage(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps &&
        ImageHosts.Any(host => host.StartsWith('.')
            ? uri.Host.EndsWith(host, StringComparison.OrdinalIgnoreCase)
            : string.Equals(uri.Host, host, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The image's URL without its size parameters (Checkers' width and height, Pick n Pay's w, h and q), so the dialog
    /// loads the full file. The rest of the query stays: a host may keep the file's key there (Woolworths' o=…), and
    /// without it there is no image at all.
    /// </summary>
    private static string WithoutSize(string url)
    {
        var builder = new UriBuilder(url);
        builder.Query = string.Join('&', builder.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.Split('=')[0] is not ("w" or "h" or "q" or "width" or "height")));
        return builder.Uri.AbsoluteUri;
    }

    private static bool IsSafeUrl(string? url) =>
        string.IsNullOrEmpty(url) ||
        url.StartsWith('#') ||
        url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
        url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);
}
