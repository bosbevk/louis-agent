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
    /// A step's number is drawn by the surface tool's <c>step()</c> from the six names each theme sets, so flat draws
    /// a squared label and glass its circle ("A: the theme decides"); it no longer wears the main button, so it does
    /// not answer the pointer as a button does.
    /// </summary>
    [Test]
    public void AStepsNumber_IsDrawnByTheStepTool_NotAsAButton()
    {
        var rule = RuleOpenedBy(Path.Combine(Web, "Components/Site/Text/Steps.razor.scss"), ".step-number {").ToList();
        string tool = string.Join('\n',
            RuleOpenedBy(Path.Combine(Web, "Styles/web/tools/surface/_marks.scss"), "@mixin step() {"));

        Assert.Multiple(() =>
        {
            Assert.That(rule, Does.Contain("    @include surface.step();"));
            Assert.That(rule, Has.None.Contains("surface.press()"));
            Assert.That(rule, Has.None.Contains("border-radius"), "the corner is the theme's");
            foreach (string name in new[] { "fill", "ink", "edge", "shadow", "radius", "face" })
                Assert.That(tool, Does.Contain($"var(--web-step-{name})"), $"the tool reads the step's {name}");
        });
    }

    /// <summary>
    /// A list of terms reads as a list of steps does, 64 characters wide in 17px words, so the two end at one edge
    /// (Louis's pick B of finding 9).
    /// </summary>
    [Test]
    public void AListOfTerms_IsAsWideAsAListOfSteps()
    {
        string sheet = Path.Combine(Web, "Components/Site/Text/TermList.razor.scss");

        Assert.Multiple(() =>
        {
            Assert.That(File.ReadLines(sheet).Single(line => line.StartsWith(".terms {", StringComparison.Ordinal)),
                Does.Contain("max-width: 64ch;"));
            Assert.That(RuleOpenedBy(sheet, ".terms li {"), Does.Contain("    font-size: 17px;"));
        });
    }

    /// <summary>
    /// The page turn's cards keep the site's gutter at their sides, so they start and end where a band's words do,
    /// not 48 wider on each side.
    /// </summary>
    [Test]
    public void ThePageTurn_KeepsTheSitesGutter() =>
        Assert.That(RuleOpenedBy(Path.Combine(TestPaths.RepoRoot,
                "src/web/LouisAgent.Web.Docs/Contents/PageTurn.razor.scss"), ".page-turn {"),
            Does.Contain("    padding-inline: var(--site-gutter, 48px);"));

    /// <summary>
    /// On a wide screen the line beside the menu is drawn on the page's column, which is as tall as the page, and no
    /// longer on the menu's, which is held in view and only as tall as the menu, so it reaches the page's foot.
    /// </summary>
    [Test]
    public void TheMenusLine_IsDrawnOnThePagesColumn()
    {
        var menu = RuleOpenedBy(DocsLayout, "    .site-docs-menu {").ToList();
        var page = RuleOpenedBy(DocsLayout, "    .site-docs-page {").ToList();

        Assert.Multiple(() =>
        {
            Assert.That(menu, Has.None.Contains("surface.rule("));
            Assert.That(page, Does.Contain("        @include surface.rule(left);"));
        });
    }

    /// <summary>
    /// F169.5: on a phone a docs page's words stand 24 from the screen's edge, the grid's 12 and the page's 12, since
    /// the layout sets the site's gutter to none, which a section and the page turn both read; a section has 48 of
    /// room above it and 32 below.
    /// </summary>
    [Test]
    public void OnAPhone_TheWordsStand24FromTheEdge()
    {
        var phone = RuleOpenedBy(DocsLayout, "@media (max-width: 720px) {").ToList();

        Assert.Multiple(() =>
        {
            Assert.That(phone, Does.Contain("    .site-docs { --site-gutter: 0px; }"));
            Assert.That(phone, Does.Contain("    .site-docs ::deep .site-main { padding-inline: 12px; }"));
            Assert.That(phone,
                Does.Contain("    .site-docs ::deep .site-main .band { padding-top: 48px; padding-bottom: 32px; }"));
        });
    }

    /// <summary>
    /// F169.5: a code word in running words may break anywhere when it would not fit, so a long path never pushes a
    /// narrow page wider than its screen. The framework's rule, for every site; a listing keeps its lines.
    /// </summary>
    [Test]
    public void ACodeWord_MayBreakAnywhere() =>
        Assert.That(File.ReadLines(Path.Combine(Web, "Styles/web/base/_elements.scss")),
            Does.Contain(":not(pre) > code { @include surface.code-word(); overflow-wrap: anywhere; }"));

    /// <summary>
    /// The lines of the rule a sheet opens with that exact line, up to the brace at the same indent that closes it.
    /// </summary>
    private static IEnumerable<string> RuleOpenedBy(string sheet, string opening)
    {
        string closing = opening[..^opening.TrimStart().Length] + "}";

        return File.ReadLines(sheet).SkipWhile(line => line != opening).TakeWhile(line => line != closing);
    }

    /// <summary>
    /// A line that gives a corner a number, such as <c>border-radius: 14px</c> or <c>$radius: 14px</c>.
    /// </summary>
    private static bool WritesANumberForItsCorner(string line)
    {
        int at = line.IndexOf("radius: ", StringComparison.Ordinal);

        return at >= 0 && char.IsDigit(line[at + "radius: ".Length]);
    }
}
