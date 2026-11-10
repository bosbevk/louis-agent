namespace LouisAgent.Web.Components.Site.Showroom.Mock.Website;

/// <summary>
/// Which page of a mock website an address is on (F127.2): the longest page address it starts with, by whole words,
/// so a section and a card are on their page (<c>/rooms/garden</c> on <c>/rooms</c>) and a page under another page is
/// its own (<c>/practice/wills</c>). An address no page holds is home.
/// </summary>
public static class MockPaths
{
    /// <summary>The site's home: <c>/</c>.</summary>
    public const string Home = "/";

    /// <summary>
    /// The page <paramref name="address"/> is on, of <paramref name="pages"/>; home when it's on none.
    /// </summary>
    /// <remarks>
    /// Guard clauses: no address is home, then the longest page it's on. A website without a home page starts on its
    /// first, so an unknown address still lands somewhere it has.
    /// </remarks>
    public static string PageOf(string? address, IEnumerable<string> pages)
    {
        var all = pages.ToList();
        string home = all.Contains(Home) || all.Count == 0 ? Home : all[0];
        if (address is null) return home;
        return all.Where(page => IsOn(address, page)).MaxBy(page => page.Length) ?? home;
    }

    /// <summary>Whether an address is the page or under it; every address is under home.</summary>
    private static bool IsOn(string address, string page) =>
        page == Home || address == page || address.StartsWith(page + "/", StringComparison.Ordinal);
}
