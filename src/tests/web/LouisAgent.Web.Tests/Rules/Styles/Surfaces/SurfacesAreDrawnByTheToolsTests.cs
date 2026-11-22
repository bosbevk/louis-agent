namespace LouisAgent.Web.Tests.Rules.Styles.Surfaces;

/// <summary>
/// F151.4: every surface in the framework's web is drawn by the shared tools, which read the names a theme sets, so a
/// theme reaches every piece. A sheet draws a surface by hand when a declaration of <c>background</c>, <c>border</c>
/// on any side, <c>box-shadow</c>, <c>backdrop-filter</c> or <c>outline</c> paints something: a colour, a name, a
/// gradient, a shadow or a blur (Louis's pick A of three, and outline with it). <c>none</c>, <c>0</c>,
/// <c>transparent</c> and <c>inherit</c> clear rather than paint, and <c>border-radius</c> is shape, so none of them
/// counts. The tools and the settings are where the drawing lives, so they are not read. The libraries on the
/// framework's web are read too (F151.5): the account pages, the eval studio and the packs' views. No sheet of any of
/// them draws by hand, and one that starts to fails here.
/// </summary>
public class SurfacesAreDrawnByTheToolsTests
{
    /// <summary>The framework's web, which holds the tools.</summary>
    private static readonly string Web = Path.Combine(TestPaths.RepoRoot, "src/web/LouisAgent.Web");

    /// <summary>The folders whose sheets are read: the framework's web, and the libraries built on it.</summary>
    private static readonly string[] Read =
    [
        "src/web/LouisAgent.Web", "src/web/LouisAgent.Web.Accounts", "src/web/LouisAgent.Web.Evals",
        "src/web/LouisAgent.Web.Docs", "src/web/LouisAgent.Web.Framework", "packs/web"
    ];

    /// <summary>The properties that draw a surface.</summary>
    private static readonly string[] Drawing =
    [
        "background", "background-color", "background-image",
        "border", "border-top", "border-right", "border-bottom", "border-left", "border-color",
        "border-top-color", "border-right-color", "border-bottom-color", "border-left-color",
        "box-shadow", "backdrop-filter", "-webkit-backdrop-filter", "outline", "outline-color"
    ];

    /// <summary>What in a value paints: a name, a colour written out, a gradient, a blur.</summary>
    private static readonly string[] Paints = ["var(--", "#", "rgb", "hsl", "gradient", "blur("];

    /// <summary>
    /// Every component and global sheet that is read, from the repo's root, the tools and settings left out.
    /// </summary>
    private static IEnumerable<string> Sheets() =>
        Read.SelectMany(folder => Directory.EnumerateFiles(Path.Combine(TestPaths.RepoRoot, folder), "*.scss",
                SearchOption.AllDirectories))
            .Select(file => Path.GetRelativePath(TestPaths.RepoRoot, file).Replace('\\', '/'))
            .Where(file => !file.Contains("/obj/", StringComparison.Ordinal) &&
                           !file.Contains("/bin/", StringComparison.Ordinal) &&
                           !file.Contains("/tools/", StringComparison.Ordinal) &&
                           !file.Contains("/settings/", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal);

    /// <summary>Whether a sheet has a declaration that paints a surface.</summary>
    private static bool DrawsByHand(string sheet) =>
        File.ReadLines(Path.Combine(TestPaths.RepoRoot, sheet))
            .Select(line => line.Trim())
            .Where(line => !line.StartsWith("//", StringComparison.Ordinal) &&
                           !line.StartsWith("/*", StringComparison.Ordinal) &&
                           !line.StartsWith('*'))
            .SelectMany(line => line.Split(';'))
            .Any(PaintsASurface);

    /// <summary>
    /// Whether one declaration paints a surface: what follows the last opening brace, its property one of
    /// <see cref="Drawing"/>, and its value holding one of <see cref="Paints"/>.
    /// </summary>
    private static bool PaintsASurface(string text)
    {
        string declaration = text[(text.LastIndexOf('{') + 1)..].Trim();
        int colon = declaration.IndexOf(':');
        if (colon <= 0) return false;

        string property = declaration[..colon].Trim();
        string value = declaration[(colon + 1)..];
        return Drawing.Contains(property) && Paints.Any(mark => value.Contains(mark, StringComparison.Ordinal));
    }

    /// <summary>No sheet draws a surface by hand, so a theme reaches every piece and a new sheet can't slip one in.</summary>
    [Test]
    public void NoSheet_DrawsASurfaceByHand()
    {
        var byHand = Sheets().Where(DrawsByHand).ToList();

        Assert.That(byHand, Is.Empty,
            "Draw these through the surface tool, tools/_surface.scss:\n  " + string.Join("\n  ", byHand));
    }

    /// <summary>The surface tool's source: the file a sheet names, and the files of kinds it hands on.</summary>
    private static string Tool => string.Join("\n", Directory
        .EnumerateFiles(Path.Combine(Web, "Styles/web/tools/surface"), "*.scss").Order(StringComparer.Ordinal)
        .Prepend(Path.Combine(Web, "Styles/web/tools/_surface.scss")).Select(File.ReadAllText));

    /// <summary>The tool has a mixin for each kind of surface the mockups have.</summary>
    [TestCase("pane")]
    [TestCase("bar")]
    [TestCase("press")]
    [TestCase("quiet-press")]
    [TestCase("chip")]
    [TestCase("tag")]
    [TestCase("well")]
    [TestCase("inset")]
    [TestCase("field")]
    [TestCase("picked")]
    [TestCase("lifts")]
    [TestCase("opened")]
    [TestCase("float")]
    [TestCase("frosted-float")]
    [TestCase("slab")]
    [TestCase("tint")]
    [TestCase("accent-line")]
    [TestCase("marker")]
    [TestCase("progress")]
    [TestCase("veil")]
    [TestCase("rule")]
    [TestCase("hairline")]
    [TestCase("outlined")]
    [TestCase("track")]
    [TestCase("panel")]
    [TestCase("bubble")]
    [TestCase("mark")]
    [TestCase("mark-ring")]
    [TestCase("mark-arc")]
    [TestCase("mark-lit")]
    [TestCase("mark-dot")]
    [TestCase("floating-bar")]
    [TestCase("sunk")]
    [TestCase("sunk-lit")]
    [TestCase("floating-chip")]
    [TestCase("wash")]
    [TestCase("write-in")]
    [TestCase("beacon")]
    [TestCase("sweep")]
    [TestCase("tag-you")]
    [TestCase("lined")]
    [TestCase("edged")]
    [TestCase("ring")]
    [TestCase("badge")]
    [TestCase("brand-line")]
    [TestCase("lit")]
    [TestCase("cast")]
    [TestCase("press-shadow")]
    [TestCase("raised")]
    [TestCase("glowing")]
    [TestCase("focus-ring")]
    [TestCase("fades-up")]
    [TestCase("image-fill")]
    [TestCase("glint")]
    [TestCase("frosted-float")]
    [TestCase("spoken")]
    [TestCase("spoken-deep")]
    [TestCase("typed-in")]
    [TestCase("working-rim")]
    [TestCase("helper")]
    [TestCase("shimmer")]
    [TestCase("spotlight")]
    [TestCase("underline")]
    [TestCase("ground")]
    [TestCase("hovering")]
    [TestCase("grip")]
    [TestCase("depth")]
    [TestCase("minion")]
    [TestCase("page-light")]
    [TestCase("outline-colour")]
    [TestCase("spotlit")]
    [TestCase("kept-ring")]
    [TestCase("side-line")]
    public void TheTool_DrawsEachKindOfSurface(string kind) =>
        Assert.That(Tool, Does.Contain($"@mixin {kind}("));

    /// <summary>
    /// Every drawing the tool lays on a piece is one the framework names, so it reads no name a theme does not set.
    /// </summary>
    [Test]
    public void EveryDrawingTheToolReads_IsNamedByTheFramework()
    {
        string names = File.ReadAllText(Path.Combine(Web, "Styles/web/settings/_surfaces.scss"));
        var read = Tool.Split("@include drawn(").Skip(1).Select(call => call[..call.IndexOf(',')].Trim()).Distinct()
            .ToList();
        Assert.That(read, Has.Count.GreaterThan(10), "the tool's drawings were read");

        Assert.That(read.Where(drawing => !names.Contains($"--web-art-{drawing}:", StringComparison.Ordinal)),
            Is.Empty);
    }
}
