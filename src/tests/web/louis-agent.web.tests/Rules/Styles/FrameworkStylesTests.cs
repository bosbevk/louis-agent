using System.Text.RegularExpressions;
using LouisAgent.Testing;

namespace LouisAgent.Web.Tests.Rules.Styles;

/// <summary>
/// The chat's frame is drawn by the framework, so its rules live there too, and a site lists them rather than copying
/// them. They read every colour from a variable, or each new site would come out in the colours of the first.
/// </summary>
public partial class FrameworkStylesTests
{
    private static readonly string Styles = Path.Combine(TestPaths.RepoRoot, "src/web/louis-agent.web/Styles/web");

    /// <summary>The studio's rules (CL57), under the same rules as the framework's.</summary>
    private static readonly string EvalStyles =
        Path.Combine(TestPaths.RepoRoot, "src/web/louis-agent.web.evals/Styles/evals");

    /// <summary>A colour written as a value: matches <c>#f58220</c> and <c>rgba(245, 130, 32, .4)</c>, but not
    /// <c>rgba(var(--bs-primary-rgb), .4)</c>.</summary>
    [GeneratedRegex(@"#[0-9a-fA-F]{3,8}\b|rgba?\(\s*\d", RegexOptions.NonBacktracking)]
    private static partial Regex FixedColour();

    /// <summary>The three libraries, whose components may carry their own styles beside them (CL58).</summary>
    private static readonly string[] Libraries =
        ["src/web/louis-agent.web", "src/web/louis-agent.web.accounts", "src/web/louis-agent.web.evals"];

    /// <summary>
    /// The framework's rules and the two libraries', the partials a site forwards and the styles a component carries
    /// beside it (the account library has only the latter since CL58), without the colours, which are where the
    /// values belong.
    /// </summary>
    private static IEnumerable<string> Rules() =>
        Directory.EnumerateFiles(Styles, "_*.scss", SearchOption.AllDirectories)
            .Where(file => !Path.GetRelativePath(Styles, file).Replace('\\', '/')
                .StartsWith("settings/", StringComparison.Ordinal))
            .Concat(Directory.EnumerateFiles(EvalStyles, "_*.scss", SearchOption.AllDirectories))
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
    [TestCase(".run-table")]
    [TestCase(".trend-delta")]
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
    [TestCase(".site-nav { margin-left: auto;")]
    [TestCase("border-radius: 999px;\n    background: var(--web-cta);")]
    [TestCase("position: fixed;\n        left: 0;\n        right: 0;\n        bottom: 0;\n" +
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
    /// A section paged sideways keeps its heading with what follows it (FX110), so at 1080p a page never holds only a
    /// section's title while its content starts on the next.
    /// </summary>
    [Test]
    public void APagedSection_KeepsItsHeadingWithItsContent() =>
        Assert.That(string.Concat(Rules().Select(File.ReadAllText)),
            Does.Contain(".is-paged .band-body ::deep > :is(.kicker, h1, h2, h3, .lede) { break-after: avoid; }"));

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
    /// On a phone the guide opens full screen (FX115): the panel is fixed to the screen from the width the bottom bar
    /// starts at, and the bar has no backdrop filter, which would make it the box a fixed panel is placed in and
    /// squeeze the guide into the bar.
    /// </summary>
    [Test]
    public void OnAPhone_TheGuideOpensFullScreen()
    {
        string rules = string.Concat(Rules().Select(File.ReadAllText)).ReplaceLineEndings("\n");
        string shell = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/louis-agent.web/Components/Site/SiteShell.razor.scss")).ReplaceLineEndings("\n");
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
