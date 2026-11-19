namespace LouisAgent.Web.Docs.Contents;

/// <summary>
/// A docs site's contents: its pages in reading order, under their groups. The menu is drawn from it and a page's
/// previous and next are read from it, so the two can't disagree.
/// </summary>
/// <remarks>
/// A value the pages' package brings and the parts ask (ADR 0040, ADR 0123): which page an address is on, and which
/// pages stand either side of it, are answered here, never worked out where a link is drawn.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public sealed record DocsContents(IReadOnlyList<DocsGroup> Groups)
{
    /// <summary>Every page, in reading order, groups one after another.</summary>
    public IReadOnlyList<DocsEntry> Pages { get; } = [.. Groups.SelectMany(group => group.Entries)];

    /// <summary>
    /// The page an address is on: its own address, or one of its sections' under it. The root is only itself, since
    /// every address sits under it.
    /// </summary>
    public DocsEntry? FindEntry(string address)
    {
        string path = "/" + address.Trim('/');
        return Pages.Where(page => page.Holds(path)).MaxBy(page => page.Address.Length);
    }

    /// <summary>The page before the one this address is on, in reading order.</summary>
    public DocsEntry? FindBefore(string address) => FindBeside(address, -1);

    /// <summary>The page after the one this address is on, in reading order.</summary>
    public DocsEntry? FindAfter(string address) => FindBeside(address, 1);

    /// <summary>The page this many places from the one the address is on, or none at either end.</summary>
    private DocsEntry? FindBeside(string address, int step)
    {
        if (FindEntry(address) is not { } here) return null;

        int at = Pages.ToList().IndexOf(here) + step;
        return at >= 0 && at < Pages.Count ? Pages[at] : null;
    }
}
