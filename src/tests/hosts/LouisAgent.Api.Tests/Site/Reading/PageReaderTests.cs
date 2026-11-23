using LouisAgent.Api.Site.Reading;
using LouisAgent.Api.Site.Rendering;
using LouisAgent.SessionStream.Wire.Requests;

namespace LouisAgent.Api.Tests.Site.Reading;

/// <summary>
/// F126.3.3: a page rendered on the server is read as the guide's widget reads it in the browser, so an eval's agent
/// gets the page production's agent gets: its address, title, the words of its main part, its places and its links.
/// What needs a screen (the section in view, the open card, the selected piece) is never set.
/// </summary>
public class PageReaderTests
{
    /// <summary>The address is the one asked for, and the title the rendered page's.</summary>
    [Test]
    public void TheAddress_IsTheOneAskedFor_AndTheTitleTheRenderedPages()
    {
        var page = Read("<main><p>Hi.</p></main>");

        Assert.That((page.Url, page.Title), Is.EqualTo(("/pricing", "Pricing · louis-agent")));
    }

    /// <summary>The text is the words of main only, with no script or style, decoded and folded.</summary>
    [Test]
    public void TheText_IsTheWordsOfMain_WithoutScriptOrStyle_DecodedAndFolded()
    {
        var page = Read("<header><h1>The site</h1></header><main><h1>Prices</h1>\n  <p>Fish &amp; chips</p>" +
                        "<style>.x { color: red; }</style><script>var y = 1;</script><p>for   two.</p></main>");

        Assert.That(page.Text, Is.EqualTo("Prices Fish & chips for two."));
    }

    /// <summary>
    /// An element the markup marks hidden, or hidden from a screen reader, is no words and no link, as a visitor never
    /// reads it; the words around it stay.
    /// </summary>
    [Test]
    public void AnElementMarkedHidden_IsNoWords_AndNoLink()
    {
        var page = Read("<main><p>Shown.</p><div hidden><p>Page 1 of 2</p><a href=\"/next\">Next</a></div>" +
                        "<span aria-hidden=\"true\">Resting</span><div aria-hidden=\"false\">Read.</div>" +
                        "<p>End.</p></main>");

        Assert.That(page.Text, Is.EqualTo("Shown. Read. End."));
        Assert.That(page.Links, Is.Empty);
    }

    /// <summary>A page with no main gives the words of its whole body.</summary>
    [Test]
    public void APageWithNoMain_GivesTheWordsOfItsWholeBody() =>
        Assert.That(Read("<div><h1>Prices</h1><p>Ask us.</p></div>").Text, Is.EqualTo("Prices Ask us."));

    /// <summary>
    /// The areas are the sections, cards and pieces with an id, each by its id and its heading, a card marked as
    /// one that opens and a piece as one the guide selects, as the widget marks them.
    /// </summary>
    [Test]
    public void TheAreas_AreThePlaces_ByIdAndHeading_MarkedByKind()
    {
        var page = Read("<main><section id=\"plans\"><h2>Our plans</h2>" +
                        "<div id=\"small\" data-card><span class=\"card-title\">Small</span></div>" +
                        "<div id=\"cost\" data-piece><h3>The cost</h3></div></section></main>");

        Assert.That(page.Areas,
            Is.EqualTo(new[]
            {
                new PageAreaRequest("plans", "Our plans"), new PageAreaRequest("small", "Small (card, opens)"),
                new PageAreaRequest("cost", "The cost (piece, selects)")
            }));
    }

    /// <summary>
    /// The links are those within the site, read from main, each address once, by its words cut at 60; a link with
    /// no words is named by its address.
    /// </summary>
    [Test]
    public void TheLinks_AreTheSitesOwn_FromMain_EachOnce_ByTheirWords()
    {
        string longWords = new('w', 80);
        var page = Read("<nav><a href=\"/menu\">Menu</a></nav><main><a href=\"/contact\">Talk to us</a>" +
                        "<a href=\"https://example.com\">Elsewhere</a><a href=\"/contact\">Again</a>" +
                        $"<a href=\"/long\">{longWords}</a><a href=\"/icon\"><svg></svg></a></main>");

        Assert.That(page.Links,
            Is.EqualTo(new[]
            {
                new PageAreaRequest("/contact", "Talk to us"), new PageAreaRequest("/long", new string('w', 60)),
                new PageAreaRequest("/icon", "/icon")
            }));
    }

    /// <summary>A page's links stop at forty.</summary>
    [Test]
    public void TheLinks_StopAtForty()
    {
        string links = string.Concat(Enumerable.Range(1, 45).Select(n => $"<a href=\"/p{n}\">Page {n}</a>"));

        Assert.That(Read($"<main>{links}</main>").Links, Has.Count.EqualTo(40));
    }

    /// <summary>The focus is the one handed in, and none when the site names none.</summary>
    [Test]
    public void TheFocus_IsTheOneHandedIn_OrNone()
    {
        var rendered = new RenderedPage("Pricing · louis-agent", "", "<main><p>Hi.</p></main>");

        Assert.That(PageReader.Read(rendered, "/pricing", "/pricing", "selling").Focus, Is.EqualTo("selling"));
        Assert.That(PageReader.Read(rendered, "/pricing", "/pricing", null).Focus, Is.Null);
    }

    /// <summary>What needs a screen is never set: the section in view, the open card, the selected piece.</summary>
    [Test]
    public void WhatNeedsAScreen_IsNeverSet()
    {
        var page = Read("<main><section id=\"plans\"><h2>Our plans</h2></section></main>");

        Assert.That((page.InView, page.Open, page.Selected), Is.EqualTo(((PageAreaRequest?)null,
            (PageAreaRequest?)null, (PageAreaRequest?)null)));
    }

    /// <summary>A rendered pricing page of this body, read at its own address with no focus.</summary>
    private static PageRequest Read(string body) =>
        PageReader.Read(new RenderedPage("Pricing · louis-agent", "A description.", body), "/pricing", "/pricing",
            null);
}
