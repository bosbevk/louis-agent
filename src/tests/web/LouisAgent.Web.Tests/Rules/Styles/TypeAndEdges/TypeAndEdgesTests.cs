using LouisAgent.Testing;

namespace LouisAgent.Web.Tests.Rules.Styles.TypeAndEdges;

/// <summary>
/// F169.2: a docs page's type and edges, as the design review picked them. A heading is sized by its rank, the page's
/// top, a section and a part, in the docs layout's own sheet, so a site of slides keeps its one size. Read from the
/// framework's sheets, the ones <see cref="FrameworkStylesTests"/> reads.
/// </summary>
public class TypeAndEdgesTests
{
    /// <summary>The framework's library, whose shared sheets and component sheets these rules are in.</summary>
    private static readonly string Web = Path.Combine(TestPaths.RepoRoot, "src/web/LouisAgent.Web");

    /// <summary>The docs layout's own sheet, beside the component that draws its columns.</summary>
    private static readonly string DocsLayout = Path.Combine(Web, "Components/Site/Docs/DocsColumns.razor.scss");

    /// <summary>
    /// A part's title has the shape of a band's heading, its weight, balance and width, and no other h3 in a band does:
    /// a tile's, a panel's or a card's h3 keeps its own look. The part's selector stands beside the two headings, not
    /// inside their <c>:is()</c>, so the rule weighs what it weighed and a site's own heading rule still wins.
    /// </summary>
    [Test]
    public void ABandsHeadingRule_CoversAPartsTitle_AndNoOtherH3()
    {
        string rule = File.ReadLines(Path.Combine(Web, "Styles/web/base/_elements.scss"))
            .Single(line => line.StartsWith(".band :is(h1, h2)", StringComparison.Ordinal));

        Assert.That(rule, Is.EqualTo(".band :is(h1, h2), .band .part-title {"));
    }

    /// <summary>
    /// In the docs layout each rank has its own size, its phone size a floor and its desktop size a ceiling (Louis's
    /// picks A of finding 1 and "A: 40, 32, 24" for a phone), and a band has 56 of room over it where a page of slides
    /// has 88.
    /// </summary>
    [TestCase(".site-docs ::deep .band h1 { font-size: clamp(40px, 4.8vw, 74px); }")]
    [TestCase(".site-docs ::deep .band h2 { font-size: clamp(32px, 3.3vw, 40px); "
        + "line-height: 1.08; letter-spacing: -.03em; }")]
    [TestCase(".site-docs ::deep .band .part-title { font-size: clamp(24px, 2.3vw, 28px); "
        + "line-height: 1.15; letter-spacing: -.02em; }")]
    [TestCase(".site-docs ::deep .site-main .band { max-width: none; padding-top: 56px; }")]
    public void TheDocsLayout_SizesAHeadingByItsRank(string rule) =>
        Assert.That(File.ReadLines(DocsLayout), Does.Contain(rule));

    /// <summary>
    /// Each corner on a docs page reads the name for what wears it and writes no number of its own (Louis's pick B of
    /// finding 8, and "A: names each theme sets"). Orbi's panel takes the window's corner, the search window's, on a
    /// desktop and at the top of its sheet on a phone. The rule is found by its opening line, indent and all.
    /// </summary>
    [TestCase("src/web/LouisAgent.Web.Docs/Contents/DocsMenu.razor.scss", ".docs-menu a {", "small")]
    [TestCase("src/web/LouisAgent.Web/Styles/web/tools/surface/_marks.scss", "@mixin code-word() {", "small")]
    [TestCase("src/web/LouisAgent.Web/Components/Site/Code/CodeListing.razor.scss", ".code-listing pre {", "block")]
    [TestCase("src/web/LouisAgent.Web.Docs/Contents/PageTurn.razor.scss", ".page-turn a {", "block")]
    [TestCase("src/web/LouisAgent.Web/Styles/web/layout/_panels.scss", ".search-dialog {", "window")]
    [TestCase("src/web/LouisAgent.Web/Components/Widget/ChatWidget.razor.scss", ".widget-panel {", "window")]
    [TestCase("src/web/LouisAgent.Web/Components/Widget/ChatWidget.razor.scss", "    .widget-panel {", "window")]
    [TestCase("src/web/LouisAgent.Web.Docs/Links/DocLink.razor.scss", ".doc-link {", "pill")]
    [TestCase("src/web/LouisAgent.Web/Components/Site/Code/CodeListing.razor.scss", ".code-ask {", "pill")]
    public void ACorner_ReadsItsName(string sheet, string opening, string corner)
    {
        var rule = RuleOpenedBy(Path.Combine(TestPaths.RepoRoot, sheet), opening).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(rule, Has.Some.Contains($"var(--web-{corner}-radius)"));
            Assert.That(rule.Where(WritesANumberForItsCorner), Is.Empty);
        });
    }

    /// <summary>
    /// The lines of the rule a sheet opens with that exact line, up to the brace at the same indent that closes it.
    /// </summary>
    private static IEnumerable<string> RuleOpenedBy(string sheet, string opening)
    {
        string closing = opening[..^opening.TrimStart().Length] + "}";

        return File.ReadLines(sheet).SkipWhile(line => line != opening).TakeWhile(line => line != closing);
    }

    /// <summary>A line that gives a corner a number, such as <c>border-radius: 14px</c> or <c>$radius: 14px</c>.</summary>
    private static bool WritesANumberForItsCorner(string line)
    {
        int at = line.IndexOf("radius: ", StringComparison.Ordinal);

        return at >= 0 && char.IsDigit(line[at + "radius: ".Length]);
    }
}
