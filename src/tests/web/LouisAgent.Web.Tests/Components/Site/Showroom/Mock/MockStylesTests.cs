using System.Text.RegularExpressions;

namespace LouisAgent.Web.Tests.Components.Site.Showroom.Mock;

/// <summary>
/// F127.2: how a mock website and its agent's chat draw themselves, from what the phone check and the live site found
/// on 2026-11-10: the spotlight's ring keeps off its neighbour, the chat sits above the whole mock, a narrow mock keeps
/// its website in view, and the chat pops in the business's colour.
/// </summary>
public partial class MockStylesTests
{
    private static readonly string Mock = Path.Combine(TestPaths.RepoRoot,
        "src/web/LouisAgent.Web/Components/Site/Showroom/Mock");

    /// <summary>A component's own styles, its line ends made plain.</summary>
    private static string Styles(string file) =>
        File.ReadAllText(Path.Combine(Mock, file)).ReplaceLineEndings("\n");

    /// <summary>The mock tool, which draws what only a mock website has, in the business's colours.</summary>
    private static string Tool() =>
        File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "src/web/LouisAgent.Web/Styles/web/tools/_mock.scss"))
            .ReplaceLineEndings("\n");

    /// <summary>The rule a selector opens, up to its closing brace.</summary>
    private static string Rule(string styles, string selector)
    {
        int start = styles.IndexOf(selector + " {", StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), $"no rule for {selector}");
        return styles[start..styles.IndexOf('}', start)];
    }

    /// <summary>The block a narrow mock's container query opens, up to its closing brace.</summary>
    private static string Narrow(string styles)
    {
        int start = styles.IndexOf("@container (max-width: 760px) {", StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), "no narrow mock's rules");
        return styles[start..styles.IndexOf("\n}", start, StringComparison.Ordinal)];
    }

    /// <summary>A z-index and its number: <c>z-index: 2</c>.</summary>
    [GeneratedRegex(@"z-index:\s*(\d+)")]
    private static partial Regex ZIndex();

    /// <summary>
    /// The spotlight's ring starts at its section's own top line (Louis's pick of the four drawn), so it never reaches
    /// the photo of the section above; its sides and bottom still reach out.
    /// </summary>
    [Test]
    public void TheSpotlightsRing_StartsAtItsSectionsOwnLine()
    {
        string ring = Rule(Styles("SiteMock.razor.scss"), ".mock-page ::deep .mock-section.is-shown::after");

        Assert.That(ring, Does.Contain("inset: 0 -8px -8px;").And.Not.Contain("inset: -8px;"));
    }

    /// <summary>
    /// The chat sits above everything the mock draws (the phone check: the hero photo's badge drew over its head).
    /// </summary>
    [Test]
    public void TheChat_SitsAboveEverythingTheMockDraws()
    {
        int chat = int.Parse(ZIndex().Match(Rule(Styles("MockWidget.razor.scss"), ".mock-widget")).Groups[1].Value);
        var mock = ZIndex().Matches(Styles("SiteMock.razor.scss")).Select(m => int.Parse(m.Groups[1].Value));

        Assert.That(mock, Is.All.LessThan(chat));
    }

    /// <summary>
    /// On a narrow mock the chat takes about two fifths of it (Louis's pick: "shorter chat"), 180 px of chat under its
    /// head in a 660 px mock, and the page's room under its last place shrinks to match, so the place the agent shows
    /// stays in view above the chat.
    /// </summary>
    [Test]
    public void OnANarrowMock_TheChatLeavesTheWebsiteInView()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Narrow(Styles("MockWidget.razor.scss")), Does.Contain("--recorded-height: 180px;"));
            Assert.That(Narrow(Styles("SiteMock.razor.scss")), Does.Contain("padding: 0 16px 280px;"));
        });
    }

    /// <summary>
    /// A page change fades the new page in (still, for less motion), and on a narrow mock the menu stays, a row of its
    /// own that scrolls sideways, so a visitor on a phone can click through the pages too.
    /// </summary>
    [Test]
    public void APage_FadesIn_AndANarrowMockKeepsItsMenu()
    {
        string page = Styles("Website/MockPage.razor.scss");

        Assert.Multiple(() =>
        {
            Assert.That(Rule(page, ".mock-view"), Does.Contain("animation: mock-page-in"));
            Assert.That(page, Does.Contain("@media (prefers-reduced-motion: reduce) {\n" +
                                           "    .mock-view { animation: none; }"));
            Assert.That(Narrow(Styles("Head/MockHead.razor.scss")),
                Does.Not.Contain(".mock-nav { display: none; }").And.Contain("overflow-x: auto;"),
                "the header lays its menu out itself since step 9");
        });
    }

    /// <summary>
    /// A page change loads like a browser: a loading line in the business's colour runs under the bar, the new
    /// address glows, and the new page slides up into place; for less motion, only the address changes.
    /// </summary>
    [Test]
    public void APageChange_LoadsLikeABrowser()
    {
        string mock = Styles("SiteMock.razor.scss");
        string page = Styles("Website/MockPage.razor.scss");

        Assert.Multiple(() =>
        {
            Assert.That(Rule(mock, ".mock-loading"),
                Does.Contain("@include mock.loading();").And.Contain("animation: mock-loading"));
            Assert.That(Tool(), Does.Contain("@mixin loading() {\n    background: var(--mock-brand);"));
            Assert.That(Rule(mock, ".mock-address.is-new"), Does.Contain("animation: mock-address-new"));
            Assert.That(mock, Does.Contain("@media (prefers-reduced-motion: reduce) {\n" +
                                           "    .mock-loading { display: none; }"));
            Assert.That(page, Does.Contain("from { opacity: 0; transform: translateY(18px); }"));
        });
    }

    /// <summary>
    /// Minimised, the chat shows only its head; maximised, it covers the whole mock, a phone's narrow one too.
    /// </summary>
    [Test]
    public void TheChat_FoldsToItsHead_OrCoversTheWebsite()
    {
        string styles = Styles("MockWidget.razor.scss");

        Assert.Multiple(() =>
        {
            Assert.That(Rule(styles, ".mock-widget.is-min .mock-widget-body"), Does.Contain("display: none;"));
            Assert.That(Rule(styles, ".mock-widget.is-max"),
                Does.Contain("inset: 12px;").And.Contain("width: auto;").And.Contain("@include fills;"));
        });
    }

    /// <summary>
    /// The chat pops (Louis: "make the chat pop a bit, looks a bit too flat"): a glow in the business's colour, a head
    /// in a gradient of that one colour with a breathing live dot (still, for less motion), and the agent's answers in
    /// bubbles of their own.
    /// </summary>
    [Test]
    public void TheChat_Pops_InTheBusinesssColour()
    {
        string styles = Styles("MockWidget.razor.scss");

        Assert.Multiple(() =>
        {
            Assert.That(Rule(styles, ".mock-widget"), Does.Contain("@include mock.widget();"));
            Assert.That(Tool(), Does.Contain("0 0 44px -8px tone(brand, 75%);"));
            Assert.That(Rule(styles, ".mock-widget-head"), Does.Contain("@include mock.widget-head();"));
            Assert.That(Tool(), Does.Contain("@mixin widget-head() {\n    background: linear-gradient(135deg"));
            Assert.That(Rule(styles, ".mock-live"), Does.Contain("animation: mock-online"));
            Assert.That(styles, Does.Contain("@media (prefers-reduced-motion: reduce) {\n" +
                                             "    .mock-live { animation: none; }"));
            Assert.That(Rule(styles, ".mock-widget-body ::deep .assistant-message .markdown"),
                Does.Contain("border-radius: 14px 14px 14px 4px;"));
        });
    }

    /// <summary>
    /// A website is as big as its window (Louis: "each inner website, needs to be expanded, and made pretty and
    /// bigger", and "everything must kinda fit 1080p so if we must make fontsizes in the demo sites smaller then do
    /// it"). The mock names one size that follows its own width or its height, whichever asks for less, its header and
    /// its page take it, and what they hold is sized in em. So the website grows with its window and never sits small
    /// in it, and on a 1080p screen its hero and its facts still fit the window.
    /// </summary>
    [Test]
    public void TheWebsite_GrowsWithItsWindow()
    {
        string mock = Styles("SiteMock.razor.scss");

        Assert.Multiple(() =>
        {
            Assert.That(Rule(mock, ".site-mock"), Does.Contain("--mock-size: clamp(12px, min(.8cqi, 2.6cqb), 17px);"));
            Assert.That(Rule(mock, ".mock-page"), Does.Contain("font-size: var(--mock-size);"));
            Assert.That(Rule(Styles("Head/MockHead.razor.scss"), ".mock-header"),
                Does.Contain("font-size: var(--mock-size);"));
            Assert.That(Rule(Styles("Hero/MockHero.razor.scss"), ".mock-hero-line"), Does.Contain("font-size: 3.2em;"));
            Assert.That(Rule(Styles("Places/MockSection.razor.scss"), ".mock-section-heading"),
                Does.Contain("2em/1.15 var(--mock-face)"));
        });
    }

    /// <summary>
    /// A photo on the website is a photo's shape, three by two, and as wide as its half of the page. It was capped at
    /// 200 px high, which on a wide window made a thin strip of it.
    /// </summary>
    [Test]
    public void APhoto_KeepsItsShape_AndIsNeverAStrip()
    {
        string photo = Rule(Styles("Places/MockSection.razor.scss"), ".mock-section-photo img");

        Assert.That(photo, Does.Contain("aspect-ratio: 3 / 2;").And.Not.Contain("max-height"));
    }

    /// <summary>
    /// The window fills the place it is given: on a stage that gives it the screen's height it grows to it, and it
    /// never gets shorter than a website can be read in. On a phone it stays tall, since the chat takes its foot,
    /// and its words stay a phone's size.
    /// </summary>
    [Test]
    public void TheWindow_FillsThePlaceItIsGiven()
    {
        string styles = Styles("SiteMock.razor.scss");

        Assert.Multiple(() =>
        {
            Assert.That(Rule(styles, ".site-mock"), Does.Contain("flex: 1 1 0;").And.Contain("min-height: 460px;"));
            Assert.That(styles, Does.Contain("@media (max-width: 900px) {\n" +
                                             "    .site-mock { --mock-size: 14px; min-height: 660px; }"));
        });
    }

    /// <summary>
    /// On a wide window the chat stands beside the website, a column of its own under the browser's bar, as the K
    /// mockup drew it, so it covers none of the website. Folded to its head it floats in the corner again.
    /// </summary>
    [Test]
    public void OnAWideWindow_TheChatStandsBesideTheWebsite()
    {
        string widget = Styles("MockWidget.razor.scss");
        int wide = widget.IndexOf("@container (min-width: 761px) {", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(Rule(Styles("SiteMock.razor.scss"), ".site-mock"),
                Does.Contain("grid-template-columns: minmax(0, 1fr) auto;"));
            Assert.That(wide, Is.GreaterThanOrEqualTo(0), "no wide window's rules");
            Assert.That(Rule(widget[Math.Max(wide, 0)..], ".mock-widget:not(.is-min):not(.is-max)"),
                Does.Contain("position: static;").And.Contain("grid-column: 2;").And.Contain("grid-row: 2 / 4;"));
            Assert.That(Rule(widget, ".mock-widget"), Does.Contain("position: absolute;"), "folded, it floats");
        });
    }
}
