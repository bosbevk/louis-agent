using System.ComponentModel;
using LouisAgent.SessionStream.Wire.Requests;

namespace LouisAgent.Evals.Files.Pages;

/// <summary>
/// A page a conversation gives in the eval file itself, as content instead of an address: its address, its title and
/// its words. It is sent with the first ask exactly as a visitor's message carries a page, so an eval can hand the
/// agent a page that isn't on the site, such as one with instructions hidden in it (F126.5.1).
/// </summary>
/// <remarks>
/// Louis's pick, 2026-11-24, "A page given in the eval file", over a test page on the site that a setting keeps out of
/// production and over the instructions in the ask itself: the content the model reads is the test's own fixture, and
/// nothing of it is ever on a live page.
/// </remarks>
/// <param name="Url">The page's address on the site, starting with <c>/</c>.</param>
/// <param name="Text">The page's words, as the agent reads them.</param>
/// <param name="Title">The page's title, or none.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0104-a-message-may-carry-the-page.md">ADR 0104: a message may carry the page</seealso>
public sealed record GivenPage(
    [property: Description("The page's address on the site, starting with /.")]
    string Url,
    [property: Description("The page's words, as the agent reads them.")]
    string Text,
    [property: Description("The page's title.")]
    string? Title = null)
{
    /// <summary>The page as a visitor's message carries it: its address, title and words, and nothing more.</summary>
    public PageRequest AsSent() => new(Url, Title, Text);
}
