using System.Reflection;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.CodeReview.Content;

/// <summary>
/// The principles every agent reads first (ADR 0020), as the site shows them (FX74): the engine's own file, embedded
/// by link, read once, its front matter dropped, the rest rendered as the chat renders markdown.
/// </summary>
/// <remarks>
/// A plain class, so the text and the rendering are tested without a component, and <c>PrinciplesTests</c> can hold
/// the embedded copy byte-equal to the file in the engine: what a visitor reads is what the model reads.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0020-principles-for-every-agent.md">ADR 0020: principles for every agent</seealso>
public static class Principles
{
    /// <summary>The file as the engine holds it, front matter included.</summary>
    public static string Markdown { get; } = Read();

    /// <summary>The principles rendered for the page: the body without its front matter.</summary>
    public static MarkupString Html { get; } = Web.Text.Markdown.ToHtml(Body(Markdown));

    private static string Read()
    {
        using var stream = typeof(Principles).Assembly.GetManifestResourceStream("principles.md")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>What follows the front matter: the text after the second <c>---</c> line.</summary>
    public static string Body(string markdown)
    {
        var lines = markdown.ReplaceLineEndings("\n").Split('\n');
        if (lines.Length == 0 || lines[0] != "---") return markdown;

        int close = Array.IndexOf(lines, "---", 1);
        return close < 0 ? markdown : string.Join('\n', lines[(close + 1)..]).Trim();
    }
}
