using LouisAgent.Web.Shared.Chats;
using System.Text.Json;
using System.Text.RegularExpressions;
using LouisAgent.Web.Shared;

namespace LouisAgent.Puls.Content;

/// <summary>
/// The sources of a turn (F20): the links in its answer, and the pages its specialists fetched. Read from the turn the
/// app already has, so nothing new is asked of the API.
/// </summary>
public static partial class Sources
{
    /// <summary>The turn's sources, in the order they first appear, each address once.</summary>
    public static List<Source> Of(Turn turn)
    {
        var found = new List<Source>();
        void Add(string url, string? title)
        {
            url = url.TrimEnd('.', ',', ';', ':');
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) return;
            if (found.Any(s => s.Url.Equals(url, StringComparison.OrdinalIgnoreCase))) return;
            string site = uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? uri.Host[4..] : uri.Host;
            found.Add(new Source(url, string.IsNullOrWhiteSpace(title) || title == url ? site : title.Trim(), site));
        }

        foreach (var block in turn.Blocks)
        {
            if (block.Kind == BlockKind.Text)
            {
                foreach (Match link in MarkdownLink().Matches(block.Text)) Add(link.Groups["url"].Value, link.Groups["title"].Value);
                foreach (Match bare in BareUrl().Matches(block.Text)) Add(bare.Value, null);
            }
            // The pages the specialist read (FetchUrl's or ReadRenderedPage's url), even when the answer doesn't link them.
            foreach (var step in block.SubTurn?.Blocks.Where(b => b.ToolName is "FetchUrl" or "ReadRenderedPage") ?? [])
                if (Url(step.ToolInput) is { } fetched) Add(fetched, null);
        }
        return found;
    }

    private static string? Url(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var input = JsonDocument.Parse(json);
            return input.RootElement.TryGetProperty("url", out var url) ? url.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    [GeneratedRegex(@"\[(?<title>[^\]]{1,200})\]\((?<url>https?://[^)\s]+)\)")]
    private static partial Regex MarkdownLink();

    [GeneratedRegex(@"(?<![(\[""])https?://[^\s)\]""<>]+")]
    private static partial Regex BareUrl();
}
