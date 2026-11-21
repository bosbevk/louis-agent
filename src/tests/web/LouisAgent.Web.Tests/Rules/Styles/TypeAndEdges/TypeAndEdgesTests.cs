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
}
