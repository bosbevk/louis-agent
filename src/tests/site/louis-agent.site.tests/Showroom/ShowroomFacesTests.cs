using System.Text.RegularExpressions;
using LouisAgent.Choices;
using LouisAgent.Web.Components.Site.Showroom.Mock;

namespace LouisAgent.Site.Tests.Showroom;

/// <summary>
/// F127.2 step 9 (Louis: "world class and fancy", his pick: self-host the fonts): the faces the showroom's brands wear,
/// as the site's build has them. Each brand's class gives its three faces (a display face for its wordmark and
/// headings, a text face, a face for small capitals), and each face is the framework's own file, beside its open
/// licence, so no third party sees a visitor.
/// </summary>
public partial class ShowroomFacesTests
{
    private static readonly SassStylesheet Css = SassStylesheet.Of("louis-agent.site");

    private static readonly string FrameworkFiles =
        Path.Combine(TestPaths.RepoRoot, "src/web/louis-agent.web/wwwroot");

    /// <summary>
    /// The framework's file a face points at: it matches
    /// <c>url("../_content/louis-agent.web/fonts/showroom/jost/jost.woff2")</c> and takes
    /// <c>fonts/showroom/jost/jost.woff2</c>, and doesn't match <c>url("https://fonts.gstatic.com/s/jost.woff2")</c>.
    /// </summary>
    [GeneratedRegex("""url\("\.\./_content/louis-agent\.web/(?<file>[^"]+\.woff2)"\)""")]
    private static partial Regex FrameworkFile();

    /// <summary>One face as the build declares it, from its <c>@font-face</c> to its closing brace.</summary>
    [GeneratedRegex(@"@font-face\s*\{[^}]*\}")]
    private static partial Regex FontFace();

    /// <summary>The rule a selector opens in the build, up to its closing brace.</summary>
    private static string Rule(string selector)
    {
        var css = Css.Read();
        var start = css.IndexOf(selector + " {", StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), $"no rule for {selector}");
        return css[start..css.IndexOf('}', start)];
    }

    /// <summary>
    /// Each brand's faces have their three roles, its display face first (mockup 0): a coastal boutique, a farm
    /// bistro, an established firm, each with a fallback a browser already has.
    /// </summary>
    [TestCase(MockFont.Cormorant, "Cormorant Garamond", "Jost", "Jost")]
    [TestCase(MockFont.Gloock, "Gloock", "Lora", "Josefin Sans")]
    [TestCase(MockFont.Bodoni, "Bodoni Moda", "Hanken Grotesk", "Hanken Grotesk")]
    public void ABrandsFaces_HaveTheirThreeRoles(MockFont font, string display, string text, string caps)
    {
        var rule = Rule($".font-{font.Wire()}");

        // Each family keeps its quotes: a name such as "Bodoni 72" is no family without them.
        Assert.Multiple(() =>
        {
            Assert.That(rule, Does.Contain($"--mock-face: \"{display}\", "));
            Assert.That(rule, Does.Contain($"--mock-text: \"{text}\", "));
            Assert.That(rule, Does.Contain($"--mock-caps: \"{caps}\", "));
        });
    }

    /// <summary>Every face a mock can wear has its class, the plain ones too.</summary>
    [Test]
    public void EveryMockFont_HasItsClass() =>
        Assert.That(Enum.GetValues<MockFont>().Where(font => !Css.Read().Contains($".font-{font.Wire()} {{")),
            Is.Empty);

    /// <summary>
    /// The faces are the framework's own files: each declared face is in its wwwroot beside its family's open
    /// licence, shows its fallback until it loads, and nothing comes from another host.
    /// </summary>
    [Test]
    public void EveryFace_IsTheFrameworksOwn_WithItsLicence()
    {
        var faces = FontFace().Matches(Css.Read()).Select(face => face.Value).ToList();
        var files = faces.Select(face => FrameworkFile().Match(face).Groups["file"].Value)
            .Select(file => Path.Combine(FrameworkFiles, file)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(faces, Has.Count.EqualTo(9), "seven families, two with their italics");
            Assert.That(files.Where(file => !File.Exists(file)), Is.Empty);
            Assert.That(files.Where(file => !File.Exists(Path.Combine(Path.GetDirectoryName(file)!, "OFL.txt"))),
                Is.Empty, "each beside its licence");
            Assert.That(faces.Where(face => !face.Contains("font-display: swap")), Is.Empty);
            Assert.That(faces.Where(face => face.Contains("http")), Is.Empty, "no other host");
        });
    }

    /// <summary>Every family a brand names is a face the build declares.</summary>
    [TestCase("Cormorant Garamond")]
    [TestCase("Jost")]
    [TestCase("Gloock")]
    [TestCase("Lora")]
    [TestCase("Josefin Sans")]
    [TestCase("Bodoni Moda")]
    [TestCase("Hanken Grotesk")]
    public void ABrandsFamily_IsDeclared(string family) =>
        Assert.That(FontFace().Matches(Css.Read()).Select(face => face.Value),
            Has.Some.Contain($"font-family: \"{family}\""));
}
