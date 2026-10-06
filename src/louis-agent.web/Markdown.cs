using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Components;

namespace louis_agent.web;

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

        // For the same reason, only plain web/mail links survive (no javascript: or data: URLs).
        foreach (var link in document.Descendants<LinkInline>())
        {
            if (!IsSafeUrl(link.Url)) link.Url = "#";
        }

        foreach (var link in document.Descendants<AutolinkInline>())
        {
            if (!IsSafeUrl(link.Url)) link.Url = "#";
        }

        return new MarkupString(document.ToHtml(Pipeline));
    }

    private static bool IsSafeUrl(string? url) =>
        string.IsNullOrEmpty(url) ||
        url.StartsWith('#') ||
        url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
        url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);
}
