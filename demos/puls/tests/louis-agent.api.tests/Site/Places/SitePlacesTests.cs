using LouisAgent.Api.Site;
using LouisAgent.Api.Site.Places;
using LouisAgent.Api.Site.Rendering;
using LouisAgent.Composition.Sites;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace LouisAgent.Api.Tests.Site.Places;

/// <summary>
/// F127.7, Louis: "they should also get Page Urls all of them inorder for the agent to go from one page to another and
/// selct it"; his question, "is there not a better way" than a hand-kept map. The site's own pages, rendered as the
/// prerender renders them, are read into an index of every place: each section, card and piece at its own address
/// (ADR 0107), with its heading and its first words, so the guide can find one on any page.
/// </summary>
public class SitePlacesTests
{
    /// <summary>A page with a section, a piece and a card, as the framework's web draws them.</summary>
    [Route("/places")]
    public sealed class PlacesPage : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddMarkupContent(0,
            "<section id=\"intro\" class=\"band\"><p class=\"kicker reveal\">How it answers</p>" +
            "<h2>Every question, day and night</h2>" +
            "<article class=\"panel stat\" id=\"0-66-0-13\" data-piece><span class=\"stat-value chrome\">" +
            "$0.66 → $0.13</span><span class=\"stat-note\">a benchmark run before and after prompt caching</span>" +
            "</article>" +
            "<button type=\"button\" id=\"the-guide\" data-card data-piece class=\"card-summary\">" +
            "<span class=\"card-title\">The guide</span><span>Louis in the corner</span></button></section>");
    }

    /// <summary>The index over this assembly's routed pages, the landing's places under the home word.</summary>
    private static SitePlaces Index() =>
        new(SiteRoutes.Of(typeof(SitePlacesTests).Assembly, "home"), new PagePrerenderer(_ => { }));

    /// <summary>Each section, piece and card of a page is a place at its own address, by its heading.</summary>
    [Test]
    public async Task EveryPlaceOfAPage_IsIndexed_AtItsAddress()
    {
        var places = await Index().AllAsync();

        Assert.That(places.Where(place => place.Address.StartsWith("/places/", StringComparison.Ordinal)),
            Is.EqualTo(new[]
            {
                new SitePlace("/places/intro", "How it answers", SitePlaceKind.Section,
                    "How it answers Every question, day and night $0.66 → $0.13 a benchmark run before and after " +
                    "prompt caching The guide Louis in the corner"),
                new SitePlace("/places/0-66-0-13", "$0.66 → $0.13", SitePlaceKind.Piece,
                    "$0.66 → $0.13 a benchmark run before and after prompt caching"),
                new SitePlace("/places/the-guide", "The guide", SitePlaceKind.Card, "The guide Louis in the corner"),
            }));
    }

    /// <summary>A search finds the place whose words hold the visitor's, its heading first.</summary>
    [Test]
    public async Task FindingWords_GivesThePlacesThatHoldThem()
    {
        var found = await Index().FindAsync("prompt caching", 2);

        Assert.That(found.Select(place => place.Address),
            Is.EqualTo(new[] { "/places/0-66-0-13", "/places/intro" }));
    }

    /// <summary>
    /// A page that fails to render (the site pages' test has one, /broken) is left out, the rest kept.
    /// </summary>
    [Test]
    public async Task APageThatFails_IsLeftOut()
    {
        var addresses = (await Index().AllAsync()).Select(place => place.Address).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(addresses, Has.None.StartsWith("/broken"));
            Assert.That(addresses, Has.Some.StartsWith("/places/").And.Some.EqualTo("/sample/top"));
        });
    }
}
