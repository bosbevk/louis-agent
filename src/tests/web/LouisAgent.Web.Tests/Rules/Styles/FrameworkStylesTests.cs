using System.Text.RegularExpressions;
using LouisAgent.Testing;

namespace LouisAgent.Web.Tests.Rules.Styles;

/// <summary>
/// The chat's frame is drawn by the framework, so its rules live there too, and a site lists them rather than copying
/// them. They read every colour from a variable, or each new site would come out in the colours of the first.
/// </summary>
public partial class FrameworkStylesTests
{
    private static readonly string Styles = Path.Combine(TestPaths.RepoRoot, "src/web/LouisAgent.Web/Styles/web");

    /// <summary>A colour written as a value: matches <c>#f58220</c> and <c>rgba(245, 130, 32, .4)</c>, but not
    /// <c>rgba(var(--bs-primary-rgb), .4)</c>.</summary>
    [GeneratedRegex(@"#[0-9a-fA-F]{3,8}\b|rgba?\(\s*\d", RegexOptions.NonBacktracking)]
    private static partial Regex FixedColour();

    /// <summary>The two libraries, whose components may carry their own styles beside them (CL58).</summary>
    private static readonly string[] Libraries =
        ["src/web/LouisAgent.Web", "src/web/LouisAgent.Web.Accounts"];

    /// <summary>
    /// The framework's rules and the two libraries', the partials a site forwards and the styles a component carries
    /// beside it (the account library has only the latter since CL58), without the colours, which are where the
    /// values belong.
    /// </summary>
    internal static IEnumerable<string> Rules() =>
        Directory.EnumerateFiles(Styles, "_*.scss", SearchOption.AllDirectories)
            .Where(file => !Path.GetRelativePath(Styles, file).Replace('\\', '/')
                .StartsWith("settings/", StringComparison.Ordinal))
            .Concat(Libraries.SelectMany(library => Directory.EnumerateFiles(
                Path.Combine(TestPaths.RepoRoot, library), "*.razor.scss", SearchOption.AllDirectories)));

    /// <summary>The parts the framework draws have their rules in the framework.</summary>
    [TestCase(".sidebar")]
    [TestCase(".conversation")]
    [TestCase(".composer")]
    [TestCase(".delegation")]
    [TestCase(".work-item")]
    [TestCase(".dropdown-item")]
    [TestCase(".admin-table")]
    [TestCase(".account-card")]
    public void TheFramesRules_AreInTheFramework(string selector) =>
        Assert.That(string.Concat(Rules().Select(File.ReadAllText)), Does.Contain(selector));

    /// <summary>The folded work panel loses its border, or that 1px gives the whole page scrollbars.</summary>
    [Test]
    public void TheFoldedWorkPanel_HasNoBorder() =>
        Assert.That(string.Concat(Rules().Select(File.ReadAllText)),
            Does.Contain(".work-hidden .work { visibility: hidden; border: 0; }"));

    /// <summary>
    /// On a phone the chat widget's panel is a sheet over the bottom half of the screen (FX116, Louis: "only half the
    /// screen must be agent chat space"), so the page the guide walks stays in view above it, and the round button
    /// goes while it is open (FX69): only the head's × closes it.
    /// </summary>
    [TestCase(".chat-widget.is-open .widget-button { display: none; }")]
    [TestCase("animation: widget-pulse")]
    [TestCase(".widget-button::before {")]
    [TestCase("@keyframes widget-pulse {\n    0%, 100% { opacity: 0; }\n    50% { opacity: 1; }\n}")]
    [TestCase(".site-nav {\n    margin-left: auto;")]
    [TestCase("@include surface.beacon();\n    @include surface.press();")]
    [TestCase("position: fixed;\n        top: auto;\n        left: 0;\n        right: 0;\n        bottom: 0;\n" +
              "        width: 100vw;\n        height: 50dvh;")]
    [TestCase(".site-corner { position: fixed; right: 24px; bottom: 72px;")]
    [TestCase(".markdown p { margin: 0 0 10px; }")]
    public void TheWidgetsPanel_FillsAPhone(string rule) =>
        Assert.That(string.Concat(Rules().Select(File.ReadAllText)).ReplaceLineEndings("\n"), Does.Contain(rule));

    /// <summary>
    /// The spotlight the guide puts on an area (F118, Louis: "more prominent and elegant"): one element over the area
    /// that dims the page around it and rings it in the accent, catches no pointer, and stays under the site's
    /// corner so the guide's words are never dimmed; it fades, unless the visitor asked for less motion.
    /// </summary>
    [TestCase("html:has(.layout-slides) { scroll-snap-type: y mandatory; }")]
    [TestCase(".layout-slides .site-main ::deep > section {")]
    [TestCase(".band.is-paged { display: block; }")]
    [TestCase("columns: 1;")]
    [TestCase(".area-spotlight {\n    position: absolute;\n    z-index: 9;\n    pointer-events: none;")]
    [TestCase("0 0 0 200vmax rgba(var(--web-backdrop-rgb), .62)")]
    [TestCase("animation: area-spotlight 4s var(--web-ease) both;")]
    [TestCase("@media (prefers-reduced-motion: reduce) {\n    .area-spotlight { animation: none; }")]
    public void TheSpotlight_DimsThePage_AndRingsTheArea(string rule) =>
        Assert.That(string.Concat(Rules().Select(File.ReadAllText)).ReplaceLineEndings("\n"), Does.Contain(rule));

    /// <summary>
    /// The spotlight stays on the screen (F127.1, Louis: "make sure this works", a tour of a demo page): the script
    /// keeps the ring inside the screen's width, and a ring that fills it comes in without growing, so a place as wide
    /// as the screen never gives the page a sideways scroll. The script itself is checked in the browser.
    /// </summary>
    [Test]
    public void TheSpotlight_StaysOnTheScreen()
    {
        string rules = string.Concat(Rules().Select(File.ReadAllText)).ReplaceLineEndings("\n");
        string script = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/LouisAgent.Web/Components/Widget/ChatWidget.razor.js"));

        Assert.Multiple(() =>
        {
            Assert.That(rules, Does.Contain(".area-spotlight.is-wide { animation-name: area-spotlight-wide; }"));
            Assert.That(script, Does.Contain("document.documentElement.clientWidth")
                .And.Contain("light.classList.toggle(\"is-wide\""));
        });
    }

    /// <summary>
    /// A section paged sideways keeps its topic with its heading (FX110), and its heading on the first page (FX246):
    /// the summary may end a page, and a row of tiles or a list may break between its items, so the browser never has
    /// to give up the whole chain from the topic down and leave the topic alone on a page.
    /// </summary>
    [Test]
    public void APagedSection_KeepsItsHeadingOnTheFirstPage()
    {
        string rules = string.Concat(Rules().Select(File.ReadAllText));

        Assert.Multiple(() =>
        {
            Assert.That(rules,
                Does.Contain(".is-paged .band-body ::deep > :is(.topic, h1, h2, h3) { break-after: avoid; }"));
            Assert.That(rules, Does.Contain(".is-paged .band-body ::deep > :is(.tiles, ol, ul) { break-inside: auto; }"));
        });
    }

    /// <summary>
    /// A card the frame's script finds cut in two by a page break, but that fits on a page, starts on the next page
    /// (FX126, the Starter plan on a phone): a forced break, since Chrome ignores break-inside: avoid there. The
    /// script itself is checked in the browser.
    /// </summary>
    [Test]
    public void APagedSection_StartsAMovedCardOnTheNextPage() =>
        Assert.That(string.Concat(Rules().Select(File.ReadAllText)),
            Does.Contain(".is-paged .band-body ::deep .is-new-page { break-before: column; }"));

    /// <summary>
    /// The showroom's corner keeps off the demo (F127.1, Louis: "why is that there?"): on a wide screen it sits at the
    /// bottom of the rail, where no demo is, and the stage keeps room at its bottom besides, so a demo's offer never
    /// ends under the guide's button.
    /// </summary>
    [Test]
    public void TheShowroomsCorner_KeepsOffTheDemo()
    {
        string rules = string.Concat(Rules().Select(File.ReadAllText));

        Assert.That(rules, Does.Contain(".showroom-corner { position: fixed; left: 24px; bottom: 24px; z-index: 25; }")
            .And.Contain(".showroom-stage { min-width: 0; padding-bottom: var(--corner-room); }"));
    }

    /// <summary>
    /// An open guide takes the rail's place on a wide screen (F127.1, Louis's pick of three): the rail's column widens
    /// to the guide's width and the demos step aside, so the open panel covers none of the stage; they come back when
    /// it closes.
    /// </summary>
    [Test]
    public void AnOpenGuide_TakesTheRailsPlace()
    {
        string rules = string.Concat(Rules().Select(File.ReadAllText)).ReplaceLineEndings("\n");

        Assert.That(rules, Does.Contain(".showroom:has(~ .showroom-corner .chat-widget.is-open) {")
            .And.Contain(".showroom-body { grid-template-columns: var(--guide-width) minmax(0, 1fr); }"));
    }

    /// <summary>
    /// Docked in the rail's column, the guide has no minimise or maximise (F127.2, Louis: "minimize and maximise do
    /// not make sense here"): it already fills the column, and closing it is the way back to the demos.
    /// </summary>
    [Test]
    public void TheDockedGuide_HasNoWindowButtons()
    {
        string shell = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
                "src/web/LouisAgent.Web/Components/Site/Showroom/Shell/ShowroomShell.razor.scss"))
            .ReplaceLineEndings("\n");
        int docked = shell.IndexOf(".showroom-corner:has(.chat-widget.is-open) {", StringComparison.Ordinal);

        Assert.That(docked, Is.GreaterThan(0));
        Assert.That(shell[docked..shell.IndexOf("\n    }", docked, StringComparison.Ordinal)],
            Does.Contain("::deep .window-tools { display: none; }"));
    }

    /// <summary>
    /// The place a mock website's agent shows pops with the guide's own spotlight (F127.1, Louis: "make it pop more
    /// when the selection happens", "with an animation like we have"): the same animation, its ring in the business's
    /// colour and the rest of the mock's page dimmed, and nothing stays after it ("remove the line afterwards"); none
    /// for less motion.
    /// </summary>
    [Test]
    public void AShownPlace_PopsWithTheGuidesSpotlight()
    {
        string mock = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/LouisAgent.Web/Components/Site/Showroom/Mock/SiteMock.razor.scss")).ReplaceLineEndings("\n");
        string tool = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/LouisAgent.Web/Styles/web/tools/_mock.scss"));
        int light = mock.IndexOf(".mock-page ::deep .mock-section.is-shown::after {", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(light, Is.GreaterThan(0));
            Assert.That(mock[light..mock.IndexOf('}', light)],
                Does.Contain("animation: area-spotlight 4s var(--web-ease) both;")
                    .And.Contain("@include mock.spotlit();"));
            Assert.That(tool, Does.Contain("0 0 0 3px var(--mock-brand)").And.Contain("0 0 0 200vmax"));
            Assert.That(mock, Does.Contain("@media (prefers-reduced-motion: reduce) {\n" +
                                           "    .mock-page ::deep .mock-section.is-shown::after { display: none; }"));
        });
    }

    /// <summary>
    /// A card the agent shows is pressed, not ringed (F127.1, Louis: "this is press boxes so do not show the ring
    /// around this type rather the press, the others without the press gets the ring"): it sinks and springs back as
    /// a tap would, a ripple of the business's colour spreading from its middle; still, for less motion.
    /// </summary>
    [Test]
    public void AShownCard_IsPressed_NotRinged()
    {
        string card = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
                "src/web/LouisAgent.Web/Components/Site/Showroom/Mock/Places/MockCard.razor.scss"))
            .ReplaceLineEndings("\n");
        string mock = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/LouisAgent.Web/Components/Site/Showroom/Mock/SiteMock.razor.scss"));

        Assert.Multiple(() =>
        {
            Assert.That(card, Does.Contain("animation: mock-press").And.Contain("animation: mock-ripple"));
            Assert.That(card, Does.Contain("@media (prefers-reduced-motion: reduce) {\n" +
                                           "    .mock-card.is-shown, .mock-card.is-shown::after { animation: none; }"));
            Assert.That(mock, Does.Not.Contain("::deep .is-shown::after"), "the spotlight's ring is for sections only");
        });
    }

    /// <summary>
    /// A window's three dots wear a Mac window's colours (FX128, Louis: "those 3 dots at the top could have the mac
    /// colors"), from the framework's own red, amber and green, in that order.
    /// </summary>
    [Test]
    public void AWindowsDots_WearTheMacColours() =>
        Assert.That(string.Concat(Rules().Select(File.ReadAllText)).ReplaceLineEndings("\n"),
            Does.Contain("&:nth-child(1) { background: var(--bs-danger); }\n" +
                         "    &:nth-child(2) { background: var(--bs-warning); }\n" +
                         "    &:nth-child(3) { background: var(--web-ok); }"));

    /// <summary>
    /// The site's header stays at the top of the screen (FX111): a slide lands under it, so the room a slide leaves
    /// above itself holds the header and the menu, never the bottom of the slide before.
    /// </summary>
    [Test]
    public void TheSitesHeader_StaysOnTop() =>
        Assert.That(string.Concat(Rules().Select(File.ReadAllText)).ReplaceLineEndings("\n"),
            Does.Contain("position: sticky;\n    top: 0;"));

    /// <summary>
    /// On a phone the corner's two buttons sit side by side in a slim bar across the bottom (FX113, Louis's pick of
    /// three), and a slide's room, a paged section's page and the way up end above it, so the bar covers nothing.
    /// </summary>
    [TestCase(".site { --site-bar: 60px; padding-bottom: var(--site-bar); }")]
    [TestCase("min-height: calc(100dvh - 160px - var(--site-bar, 0px));")]
    [TestCase("height: calc(100dvh - 330px - var(--site-bar, 0px));")]
    [TestCase(".chat-widget:not(.is-open) { flex-direction: row;")]
    public void OnAPhone_TheCornerIsABarAtTheBottom(string rule) =>
        Assert.That(string.Concat(Rules().Select(File.ReadAllText)).ReplaceLineEndings("\n"), Does.Contain(rule));

    /// <summary>
    /// With the guide as a bar in the frame's corner (F151.6), the frame keeps the room the bar says it stands, on a
    /// wide screen and a phone alike: the page's content, a slide and the way up end above the bar, the footer stands
    /// at the very bottom (FX163, Louis: "the footer must always be at the bottom") and above the strip the page
    /// fades into under the bar, so its words are seen, and the corner is no floating box of its own, since the bar
    /// places itself.
    /// </summary>
    [TestCase(".site:has(.site-corner .chat-widget.is-bar) { --site-bar: var(--bar-room, 150px); padding-bottom: 0; }")]
    [TestCase(".site:has(.site-corner .chat-widget.is-bar) .site-footer { position: relative; z-index: 10; }")]
    [TestCase(".site:has(.site-corner .chat-widget.is-bar) .site-main { padding-bottom: var(--site-bar); }")]
    [TestCase(".site:has(.site-corner .chat-widget.is-bar) .site-up { bottom: calc(var(--site-bar) + 12px); }")]
    [TestCase(
        ".site-corner:has(.chat-widget.is-bar) { position: static; height: auto; padding: 0; border: 0; " +
        "background: none; }")]
    public void WithTheGuideAsABar_TheFrameKeepsItsRoom(string rule)
    {
        string shell = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/LouisAgent.Web/Components/Site/SiteShell.razor.scss")).ReplaceLineEndings("\n");

        Assert.Multiple(() =>
        {
            Assert.That(shell, Does.Contain(rule));
            Assert.That(shell.IndexOf(rule, StringComparison.Ordinal),
                Is.GreaterThan(shell.IndexOf("@media (max-width: 720px)", StringComparison.Ordinal)),
                "after the phone's rules, so it wins there too");
        });
    }

    /// <summary>
    /// The small link fixed in the screen's corner (FX173) stands only on a wide screen, and there the footer keeps
    /// room at its right end, so the link never sits on the footer's own words.
    /// </summary>
    [Test]
    public void TheCornerLink_NeverCoversTheFooter()
    {
        string shell = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/LouisAgent.Web/Components/Site/SiteShell.razor.scss")).ReplaceLineEndings("\n");

        Assert.That(shell, Does.Contain(
            "@media (min-width: 1200px) {\n    .site:has(.corner-link) .site-footer { padding-right: 160px; }\n}"));
    }

    /// <summary>
    /// On a phone the guide's bar spans the screen's foot, so the footer keeps the bar's room under its words, and
    /// the orb never sits on them.
    /// </summary>
    [Test]
    public void OnAPhone_TheFootersWordsStandAboveTheBar()
    {
        string shell = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/LouisAgent.Web/Components/Site/SiteShell.razor.scss")).ReplaceLineEndings("\n");

        Assert.That(shell, Does.Contain("@media screen and (max-width: 720px) {\n"
                                        + "    .site:has(.site-corner .chat-widget.is-bar) .site-footer {"
                                        + " padding-bottom: calc(var(--site-bar) + 12px); }\n}"));
    }

    /// <summary>
    /// On a phone the guide opens full screen (FX115): the panel is fixed to the screen from the width the bottom bar
    /// starts at, and the bar has no backdrop filter, which would make it the box a fixed panel is placed in and
    /// squeeze the guide into the bar.
    /// </summary>
    [Test]
    public void OnAPhone_TheGuideOpensFullScreen()
    {
        string rules = string.Concat(Rules().Select(File.ReadAllText)).ReplaceLineEndings("\n");
        string shell = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/LouisAgent.Web/Components/Site/SiteShell.razor.scss")).ReplaceLineEndings("\n");
        int corner = shell.IndexOf("    .site-corner {", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(rules, Does.Contain("@media (max-width: 720px) {\n    .chat-widget.is-open .widget-button"));
            Assert.That(corner, Is.GreaterThan(0));
            Assert.That(shell[corner..shell.IndexOf('}', corner)], Does.Not.Contain("backdrop-filter"));
            Assert.That(shell,
                Does.Contain(".site-corner { position: fixed; right: 24px; bottom: 72px; z-index: 25; }"),
                "above the sticky header's 20, so the open panel covers it");
        });
    }

    /// <summary>
    /// A recording's ending fits inside its window (F122, Louis: "too big just button", "the panel should leave space
    /// for the button"): the guide's button has a plain, small look a site can ask for, and the window keeps its
    /// height while the list gives up room for the ending.
    /// </summary>
    [TestCase(".guide-button.is-plain {")]
    [TestCase("height: calc(var(--recorded-height, min(58vh, 560px)) + 48px);")]
    [TestCase("flex: 1 1 auto;\n    min-height: 0;")]
    public void ARecordingsEnding_FitsInsideItsWindow(string rule) =>
        Assert.That(string.Concat(Rules().Select(File.ReadAllText)).ReplaceLineEndings("\n"), Does.Contain(rule));

    /// <summary>
    /// The menu at the end of the bar opens its list towards the page (F122, Louis: "bug", the Demos list cut off at
    /// the screen's right edge): right-aligned under its label instead of left.
    /// </summary>
    [Test]
    public void TheLastMenu_OpensItsListTowardsThePage() =>
        Assert.That(string.Concat(Rules().Select(File.ReadAllText)),
            Does.Contain(".site-drop:last-child .site-drop-list { left: auto; right: -14px; }"));

    /// <summary>
    /// What is right for a screen and wrong for paper is switched off in print (FX154). The page's two layers, its
    /// lights and what a theme draws over it, such as retro's scanlines, are fixed to the whole window, so they would
    /// be drawn again on every sheet, margins and all, and a layer that blends turns each page into a picture: a grey
    /// cast and a 13 MB file. And a page that says it is dark has its sheet painted with the browser's dark canvas,
    /// which no background rule reaches, so the margins printed black (Louis: "where is the space", "it is still
    /// black", "it must be white").
    /// </summary>
    [Test]
    public void OnPaper_ThePagesLayersAreHidden_AndTheSheetIsLight() =>
        Assert.That(string.Concat(Rules().Select(File.ReadAllText)).ReplaceLineEndings("\n"),
            Does.Contain(
                "@media print {\n    body::before, body::after { display: none; }\n" +
                "    :root { color-scheme: light; }\n}"));

    /// <summary>
    /// A link on paper takes the ink of the page it is printed on (FX154, Louis's pick of three: "The page's own
    /// ink"). It first read two names of its own from the framework's colours, and a name there is one every site
    /// has to set and one the films are recorded with, so Puls's rule and the films' check both failed.
    /// </summary>
    [Test]
    public void ALinkOnPaper_TakesThePagesInk()
    {
        string link = File.ReadAllText(Rules().Single(file => file.EndsWith("PaperLink.razor.scss",
            StringComparison.Ordinal)));
        string colours = File.ReadAllText(Path.Combine(Styles, "settings/_colours.scss"));

        Assert.Multiple(() =>
        {
            Assert.That(link, Does.Contain("color: inherit;"));
            Assert.That(link + colours, Does.Not.Contain("--web-paper"));
        });
    }

    /// <summary>
    /// F151.4: the four tools that drew a surface before the surface tool now draw through it, so the pieces that
    /// include them wear a theme: a window's frame is a pane, the chat card is set into the page, a field is a field,
    /// and a pressed look lays the picked drawing.
    /// </summary>
    [TestCase("_window.scss", "@include surface.pane(")]
    [TestCase("_chat-card.scss", "@include surface.inset(")]
    [TestCase("_field.scss", "@include surface.field(")]
    [TestCase("_pressed.scss", "@include surface.picked(")]
    public void AnOldTool_DrawsThroughTheSurfaceTool(string tool, string include) =>
        Assert.That(File.ReadAllText(Path.Combine(Styles, "tools", tool)), Does.Contain(include));

    /// <summary>No rule in the framework writes a colour as a value.</summary>
    [Test]
    public void NoRule_HoldsAFixedColour()
    {
        var found = Rules()
            .Select(file => (File: Path.GetFileName(file), Count: FixedColour().Count(File.ReadAllText(file))))
            .Where(file => file.Count > 0)
            .Select(file => $"{file.File}: {file.Count}");

        Assert.That(found, Is.Empty);
    }
}
