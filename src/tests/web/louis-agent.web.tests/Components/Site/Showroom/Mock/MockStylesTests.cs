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
        "src/web/louis-agent.web/Components/Site/Showroom/Mock");

    /// <summary>A component's own styles, its line ends made plain.</summary>
    private static string Styles(string file) =>
        File.ReadAllText(Path.Combine(Mock, file)).ReplaceLineEndings("\n");

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
            Assert.That(Narrow(Styles("SiteMock.razor.scss")), Does.Contain(".mock-page { padding: 0 16px 280px; }"));
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
                Does.Contain("inset: 12px;").And.Contain("width: auto;").And.Contain("--recorded-height: 540px;"));
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
            Assert.That(Rule(styles, ".mock-widget"),
                Does.Contain("0 0 44px -8px color-mix(in srgb, var(--mock-brand) 75%, transparent)"));
            Assert.That(Rule(styles, ".mock-widget-head"), Does.Contain("linear-gradient(135deg"));
            Assert.That(Rule(styles, ".mock-live"), Does.Contain("animation: mock-online"));
            Assert.That(styles, Does.Contain("@media (prefers-reduced-motion: reduce) {\n" +
                                             "    .mock-live { animation: none; }"));
            Assert.That(Rule(styles, ".mock-widget-body ::deep .assistant-message .markdown"),
                Does.Contain("border-radius: 14px 14px 14px 4px;"));
        });
    }
}
