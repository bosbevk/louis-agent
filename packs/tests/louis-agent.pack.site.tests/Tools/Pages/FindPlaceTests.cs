using LouisAgent.Composition.Sites;
using LouisAgent.Packs.Site.Tools;

namespace LouisAgent.Packs.Site.Tests.Tools.Pages;

/// <summary>
/// F127.7: the guide finds a place on any page of the site through a tool (Louis's approved plan, after "is there not
/// a better way" than a hand-kept map), then shows it with <c>ShowPage</c>. Each found place is one line: its address,
/// its heading and its kind.
/// </summary>
public class FindPlaceTests
{
    /// <summary>A site's places, a fixed few.</summary>
    private sealed class FixedPlaces(params SitePlace[] places) : ISitePlaces
    {
        public bool HasSite => true;

        public Task<IReadOnlyList<SitePlace>> AllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SitePlace>>(places);

        public Task<IReadOnlyList<SitePlace>> FindAsync(string words, int most,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SitePlace>>(places
                .Where(place => place.Text.Contains(words, StringComparison.OrdinalIgnoreCase)).Take(most).ToList());
    }

    private static readonly FixedPlaces Site = new(
        new SitePlace("/framework/measured/0-66-0-13", "$0.66 → $0.13", SitePlaceKind.Piece,
            "$0.66 → $0.13 a benchmark run before and after prompt caching"),
        new SitePlace("/framework/identities/the-guide", "The guide", SitePlaceKind.Card, "The guide Louis"));

    /// <summary>Each place found is a line with its address, so the next call can show it.</summary>
    [Test]
    public async Task FoundPlaces_AreListedByAddress() =>
        Assert.That(await new SiteTools(Site).FindPlace("caching"), Is.EqualTo(
            "Places on the site for \"caching\", the best first. Show one with ShowPage by its address.\n" +
            "- /framework/measured/0-66-0-13  $0.66 → $0.13 (piece)"));

    /// <summary>Nothing found says so, and what to try instead.</summary>
    [Test]
    public async Task NothingFound_SaysSo() =>
        Assert.That(await new SiteTools(Site).FindPlace("weather"), Is.EqualTo(
            "Nothing on the site matches \"weather\". Try other words, or open a page from the site's pages."));

    /// <summary>A host that serves no site has no places to find.</summary>
    [Test]
    public async Task NoSite_SaysThereIsNoIndex() =>
        Assert.That(await new SiteTools().FindPlace("caching"), Is.EqualTo(
            "This host serves no site to search. Open a page from the site's pages instead."));
}
