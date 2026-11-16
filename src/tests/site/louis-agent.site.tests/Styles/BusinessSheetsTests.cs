namespace LouisAgent.Site.Tests.Styles;

/// <summary>
/// Each made-up business's website keeps one look (F151.6, Louis's pick: "One look, its corner its own too"): a
/// sheet of its own sets the theme's names under its mock, from the business's own colours, whatever theme and
/// style the page around it wears. So its agent's corner is the business's too, in the light and in the dark.
/// </summary>
public class BusinessSheetsTests
{
    private static readonly SassStylesheet Css = SassStylesheet.Of("louis-agent.site");

    /// <summary>
    /// A business's sheet names its website by its address and starts from the four colours its page hands the
    /// mock: its brand, the words on it, its page and its ink.
    /// </summary>
    [TestCase("seemeeu", "seemeeu.example", "#0b3a4a", "#e9dcc3", "#fbf8f2", "#14262d")]
    [TestCase("wheatfield", "wheatfieldkitchen.example", "#2c4a34", "#f7f0e1", "#f7f0e1", "#2b2418")]
    [TestCase("koringland", "koringland.example", "#14213d", "#f6f2ea", "#f6f2ea", "#1b2233")]
    public void ABusinesssSheet_StartsFromItsOwnColours(
        string sheet, string address, string brand, string onBrand, string page, string ink)
    {
        string source = Css.Source($"site/showroom/{sheet}");

        Assert.That(source, Does.Contain($"\"{address}\", {brand}, {onBrand}, {page}, {ink}"));
    }

    /// <summary>
    /// The build holds each business's values under its own mock: the page's colour as the body's, its ink as the
    /// words', and its brand as what you press, so nothing in its corner is the page's theme.
    /// </summary>
    [TestCase("seemeeu.example", "#0b3a4a", "#e9dcc3", "#fbf8f2", "#14262d")]
    [TestCase("wheatfieldkitchen.example", "#2c4a34", "#f7f0e1", "#f7f0e1", "#2b2418")]
    [TestCase("koringland.example", "#14213d", "#f6f2ea", "#f6f2ea", "#1b2233")]
    public void TheBuild_SetsABusinesssValues_UnderItsOwnMock(
        string address, string brand, string onBrand, string page, string ink)
    {
        string css = Css.Read();
        int start = css.IndexOf($".site-mock[data-site=\"{address}\"] {{", StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), $"no values for {address}");
        string values = css[start..css.IndexOf('}', start)];

        Assert.Multiple(() =>
        {
            Assert.That(values, Does.Contain($"--bs-body-bg: {page};"));
            Assert.That(values, Does.Contain($"--bs-body-color: {ink};"));
            Assert.That(values, Does.Contain($"--web-press-fill: {brand};"));
            Assert.That(values, Does.Contain($"--web-press-ink: {onBrand};"));
            Assert.That(values, Does.Contain("color-scheme: light;"));
        });
    }
}
