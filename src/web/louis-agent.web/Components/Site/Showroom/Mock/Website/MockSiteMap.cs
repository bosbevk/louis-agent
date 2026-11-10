namespace LouisAgent.Web.Components.Site.Showroom.Mock.Website;

/// <summary>
/// The pages a mock website holds (F127.2), in the order its site wrote them: its menu, which page an address is on,
/// and what a page is called.
/// </summary>
/// <remarks>
/// The bookkeeping behind the TabSet wiring, apart from the mock that draws it, so <see cref="SiteMock"/> keeps the
/// flow (where a visitor's click went, the agent's next place winning over it, the scrolling) and this keeps the list.
/// </remarks>
public sealed class MockSiteMap
{
    private readonly List<MockPage> _pages = [];

    /// <summary>Whether the site wrote any pages; a site of one long page writes none.</summary>
    public bool Any => _pages.Count > 0;

    /// <summary>The pages its menu lists, in order.</summary>
    public IEnumerable<MockPage> Menu => _pages.Where(page => page.InMenu);

    /// <summary>Adds a page the first time it says it's there.</summary>
    public bool Add(MockPage page)
    {
        if (_pages.Contains(page)) return false;
        _pages.Add(page);
        return true;
    }

    /// <summary>Takes a page off when it goes.</summary>
    public void Remove(MockPage page) => _pages.Remove(page);

    /// <summary>The page an address is on.</summary>
    public string PageOf(string? address) => MockPaths.PageOf(address, _pages.Select(page => page.Path));

    /// <summary>A page's word in the menu, by its address; none for an address no page has.</summary>
    public string? LabelOf(string? path) => _pages.FirstOrDefault(page => page.Path == path)?.Label;
}
