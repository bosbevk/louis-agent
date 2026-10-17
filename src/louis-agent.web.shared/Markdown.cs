using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Shared;

public static class Markdown
{
    // Raw HTML is shown as text, never rendered: answers can quote web pages or files, and that must not run as markup.
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    public static MarkupString ToHtml(string markdown)
    {
        MarkdownDocument document = Markdig.Markdown.Parse(markdown, Pipeline);

        // For the same reason, only plain web/mail links survive (no javascript: or data: URLs). A browser loads an image
        // by itself, so a model-written image could send the chat's data to any host: only a shop's own image host is
        // shown (the cook's product photos, F33); any other image becomes a link, loaded only if the user clicks it.
        foreach (var link in document.Descendants<LinkInline>())
        {
            if (!IsSafeUrl(link.Url)) link.Url = "#";
            if (link.IsImage && !IsShopImage(link.Url)) link.IsImage = false;
            // A shop's photo opens its original in a dialog when clicked (chat.js): the same file, without the size.
            if (link.IsImage) link.GetAttributes().AddProperty("data-original", new UriBuilder(link.Url!) { Query = "" }.Uri.AbsoluteUri);
        }

        foreach (var link in document.Descendants<AutolinkInline>())
        {
            if (!IsSafeUrl(link.Url)) link.Url = "#";
        }

        return new MarkupString(document.ToHtml(Pipeline));
    }

    /// <summary>The hosts whose images are shown: the shops' catalogues the cook reads (Checkers' cards load from here).</summary>
    public static readonly IReadOnlyList<string> ImageHosts = ["catalog.sixty60.co.za"];

    private static bool IsShopImage(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps &&
        ImageHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase);

    private static bool IsSafeUrl(string? url) =>
        string.IsNullOrEmpty(url) ||
        url.StartsWith('#') ||
        url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
        url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);
}
