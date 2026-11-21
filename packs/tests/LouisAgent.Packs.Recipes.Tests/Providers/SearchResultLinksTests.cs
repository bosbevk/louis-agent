using LouisAgent.Packs.Recipes.Providers;

namespace LouisAgent.Packs.Recipes.Tests.Providers;

/// <summary>
/// Checks the rule that finds a search page's results: every <c>h2</c> or <c>h3</c> that links to the same site is a
/// hit, whatever its class. Small made-up pages show the cases the saved ones don't.
/// </summary>
public class SearchResultLinksTests
{
    private static readonly Uri Home = new("https://www.example.co.za");

    /// <summary>The hits on a page whose body is the given markup, as a name and an address each.</summary>
    private static IReadOnlyList<(string Name, string Url)> Hits(string body, int max = 50) =>
        SearchResultLinks.In($"<html><body>{body}</body></html>", Home, max)
            .Select(h => (h.Name, h.Url.AbsoluteUri)).ToList();

    /// <summary>A result heading can be an h3 as well as an h2, whatever its class.</summary>
    [Test]
    public void AnH3_CountsLikeAnH2() =>
        Assert.That(Hits("<h3 class='x'><a href='https://www.example.co.za/a/'>A</a></h3>"),
            Is.EqualTo(new[] { ("A", "https://www.example.co.za/a/") }));

    /// <summary>A share link, an advert, or a look-alike address is never a hit.</summary>
    [Test]
    public void ALinkThatLeavesTheSite_IsNeverAHit() =>
        Assert.That(Hits("""
                         <h2><a href="https://facebook.com/sharer?u=x">Share</a></h2>
                         <h2><a href="https://ads.example.com/buy">Buy</a></h2>
                         <h2><a href="https://example.co.za.evil.com/x">Look-alike</a></h2>
                         """), Is.Empty);

    /// <summary>A link with no www is on the same site as a home with one.</summary>
    [Test]
    public void TheSameSiteWithOrWithoutWww_IsTheSameSite() =>
        Assert.That(Hits("""<h2><a href="https://example.co.za/a">A</a></h2>""").Single().Url,
            Is.EqualTo("https://example.co.za/a"));

    /// <summary>A link written as a path becomes a full address on the home's site.</summary>
    [Test]
    public void ARelativeLink_IsMadeAbsoluteFromTheHome() =>
        Assert.That(Hits("""<h2><a href="/recipes/a">A</a></h2>""").Single().Url,
            Is.EqualTo("https://www.example.co.za/recipes/a"));

    /// <summary>An address that two headings link to is listed once, under the first.</summary>
    [Test]
    public void TheSameAddressTwice_IsOneHit() =>
        Assert.That(Hits("""
                         <h2><a href="https://www.example.co.za/a">A</a></h2>
                         <h3><a href="https://www.example.co.za/a">A again</a></h3>
                         """).Select(h => h.Name), Is.EqualTo(new[] { "A" }));

    /// <summary>A heading with no link, or a link with no words in it, is not a hit.</summary>
    [Test]
    public void AHeadingWithNoLink_AndALinkWithNoWords_AreNoHit() =>
        Assert.That(Hits("""
                         <h2 class="title">bobotie</h2>
                         <h2><a href="https://www.example.co.za/a"><img src="a.jpg"> </a></h2>
                         """), Is.Empty);

    /// <summary>The name is the link's words: inner tags gone, entities decoded, spacing tidied.</summary>
    [Test]
    public void TheName_IsTheLinksTextWithoutTags_DecodedAndTidied() =>
        Assert.That(Hits("""
                         <h2>
                           <a
                              href="https://www.example.co.za/a">  Mom&#8217;s <em>best</em>
                              bobotie &amp; rice </a>
                         </h2>
                         """).Single().Name, Is.EqualTo("Mom’s best bobotie & rice"));

    /// <summary>A heading that never closes is not read.</summary>
    [Test]
    public void AHeadingWithoutItsClosingTag_IsNoHit() =>
        Assert.That(Hits("""
                         <h2><a href="https://www.example.co.za/a">A
                         """), Is.Empty);

    /// <summary>
    /// Some sites write their pages minified: an address with no quotes and a space before the closing bracket. It is
    /// still a link, and the space is not part of the address.
    /// </summary>
    [Test]
    public void AnUnquotedHref_IsStillALink_WithoutTheSpaceAfterIt() =>
        Assert.That(Hits("""
                         <h2 class="w-post-elm post_title">
                           <a href=https://www.example.co.za/a/ >Top 20 Dinner Recipes</a></h2>
                         """), Is.EqualTo(new[] { ("Top 20 Dinner Recipes", "https://www.example.co.za/a/") }));

    /// <summary>
    /// A link can carry other attributes that end in <c>href</c>. Only the attribute named exactly <c>href</c> is the
    /// address.
    /// </summary>
    [Test]
    public void ADataHrefBeforeTheRealHref_IsNotTheAddress() =>
        Assert.That(Hits("""
                         <h2><a data-href="https://www.example.co.za/wrong"
                                href="https://www.example.co.za/right">A</a></h2>
                         """).Single().Url, Is.EqualTo("https://www.example.co.za/right"));

    /// <summary>Once the cap is reached the walk stops.</summary>
    [Test]
    public void TheCap_StopsTheWalk() =>
        Assert.That(Hits("""
                         <h2><a href="https://www.example.co.za/a">A</a></h2>
                         <h2><a href="https://www.example.co.za/b">B</a></h2>
                         """, 1).Select(h => h.Name), Is.EqualTo(new[] { "A" }));
}
