namespace LouisAgent.Web.Tests.Rules.Styles;

/// <summary>
/// F151.4: every surface in the framework's web is drawn by the shared tools, which read the names a theme sets, so a
/// theme reaches every piece. A sheet draws a surface by hand when a declaration of <c>background</c>, <c>border</c>
/// on any side, <c>box-shadow</c>, <c>backdrop-filter</c> or <c>outline</c> paints something: a colour, a name, a
/// gradient, a shadow or a blur (Louis's pick A of three, and outline with it). <c>none</c>, <c>0</c>,
/// <c>transparent</c> and <c>inherit</c> clear rather than paint, and <c>border-radius</c> is shape, so none of them
/// counts. The tools and the settings are where the drawing lives, so they are not read. No sheet of the framework's
/// web draws by hand, and one that starts to fails here. The libraries on it are read too (F151.5): the account pages,
/// the eval studio and the packs' views. Their sheets that still draw by hand are on a list that may only shrink.
/// </summary>
public class SurfacesAreDrawnByTheToolsTests
{
    /// <summary>The framework's web, which holds the tools.</summary>
    private static readonly string Web = Path.Combine(TestPaths.RepoRoot, "src/web/louis-agent.web");

    /// <summary>The folders whose sheets are read: the framework's web, and the libraries built on it.</summary>
    private static readonly string[] Read =
    [
        "src/web/louis-agent.web", "src/web/louis-agent.web.accounts", "src/web/louis-agent.web.evals", "packs/web"
    ];

    /// <summary>
    /// The libraries' sheets that still draw a surface by hand. A sheet leaves it, and none joins it.
    /// </summary>
    private static readonly string[] StillByHand =
    [
        "packs/web/louis-agent.pack.files.web/Styles/pack-files/_files.scss",
        "packs/web/louis-agent.pack.git.web/Styles/pack-git/_changes.scss",
        "packs/web/louis-agent.pack.triage.web/Styles/pack-triage/_demo.scss",
        "src/web/louis-agent.web.evals/Components/ChartInfo.razor.scss",
        "src/web/louis-agent.web.evals/Components/EvalsPage/Details/EvalDetails.razor.scss",
        "src/web/louis-agent.web.evals/Components/EvalsPage/Details/EvalRunControls.razor.scss",
        "src/web/louis-agent.web.evals/Components/EvalsPage/EvalChecks.razor.scss",
        "src/web/louis-agent.web.evals/Components/EvalsPage/EvalRunOrigin.razor.scss",
        "src/web/louis-agent.web.evals/Components/EvalsPage/EvalRunTimeline.razor.scss",
        "src/web/louis-agent.web.evals/Components/EvalsPage/EvalScorecard.razor.scss",
        "src/web/louis-agent.web.evals/Components/EvalsPage/List/EvalListItem.razor.scss",
        "src/web/louis-agent.web.evals/Components/Explorer/Conversations/ConversationCard.razor.scss",
        "src/web/louis-agent.web.evals/Components/Explorer/Conversations/ConversationsLevel.razor.scss",
        "src/web/louis-agent.web.evals/Components/Explorer/Head/ExplorerCrumbs.razor.scss",
        "src/web/louis-agent.web.evals/Components/Explorer/Parts/TurnTimeline.razor.scss",
        "src/web/louis-agent.web.evals/Components/LiveRun/LiveRunPanel.razor.scss",
        "src/web/louis-agent.web.evals/Components/Notes/NotesPanel.razor.scss",
        "src/web/louis-agent.web.evals/Components/Rates/ModeRatesList.razor.scss",
        "src/web/louis-agent.web.evals/Components/Reviews/AgreementPanel.razor.scss",
        "src/web/louis-agent.web.evals/Components/Reviews/CalibrationCard.razor.scss",
        "src/web/louis-agent.web.evals/Components/Reviews/Queue/ReviewCountBadge.razor.scss",
        "src/web/louis-agent.web.evals/Components/Reviews/ReviewPanel.razor.scss",
        "src/web/louis-agent.web.evals/Components/Reviews/ReviewPill.razor.scss",
        "src/web/louis-agent.web.evals/Components/RunExplorer.razor.scss",
        "src/web/louis-agent.web.evals/Components/RunPage/RunVerdict.razor.scss",
        "src/web/louis-agent.web.evals/Components/Runs/ReplayedFrom.razor.scss",
        "src/web/louis-agent.web.evals/Components/Runs/RescoredFrom.razor.scss",
        "src/web/louis-agent.web.evals/Components/Runs/RunListItem.razor.scss",
        "src/web/louis-agent.web.evals/Components/Runs/Table/RunsList.razor.scss",
        "src/web/louis-agent.web.evals/Components/Studio/DraftList.razor.scss",
        "src/web/louis-agent.web.evals/Components/Studio/Promptfoo/PromptfooPanel.razor.scss",
        "src/web/louis-agent.web.evals/Components/Studio/Try/TryStep.razor.scss",
        "src/web/louis-agent.web.evals/Components/Summary/CategoryTrends.razor.scss",
        "src/web/louis-agent.web.evals/Components/Summary/SummaryTable.razor.scss",
        "src/web/louis-agent.web.evals/Pages/Building/EvalStudio.razor.scss",
        "src/web/louis-agent.web.evals/Pages/Building/Prompts.razor.scss",
        "src/web/louis-agent.web.evals/Pages/Running/BenchmarkRun.razor.scss",
        "src/web/louis-agent.web.evals/Pages/Running/Evals.razor.scss",
        "src/web/louis-agent.web.evals/Styles/evals/_benchmarks.scss",
        "src/web/louis-agent.web.evals/Styles/evals/_evals.scss",
        "src/web/louis-agent.web.evals/Styles/evals/_explorer.scss",
        "src/web/louis-agent.web.evals/Styles/evals/_reviews.scss",
        "src/web/louis-agent.web.evals/Styles/evals/_run-page.scss",
        "src/web/louis-agent.web.evals/Styles/evals/_studio.scss"
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

    /// <summary>
    /// No sheet draws a surface by hand unless it is still on the list, so a theme reaches every piece and a new sheet
    /// can't slip one in.
    /// </summary>
    [Test]
    public void NoSheet_DrawsASurfaceByHand()
    {
        var byHand = Sheets().Where(DrawsByHand).Except(StillByHand).ToList();

        Assert.That(byHand, Is.Empty,
            "Draw these through the surface tool, tools/_surface.scss:\n  " + string.Join("\n  ", byHand));
    }

    /// <summary>
    /// A sheet that no longer draws by hand, or is gone, is taken off the list, so the list only shrinks.
    /// </summary>
    [Test]
    public void TheList_NamesNoCleanSheet()
    {
        var clean = StillByHand.Where(sheet => !File.Exists(Path.Combine(TestPaths.RepoRoot, sheet)) ||
                                               !DrawsByHand(sheet));

        Assert.That(clean, Is.Empty, "take it off the list");
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
    [TestCase("field-lit")]
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
    [TestCase("raised")]
    [TestCase("glowing")]
    [TestCase("focus-ring")]
    [TestCase("fades-up")]
    [TestCase("image-fill")]
    [TestCase("glint")]
    [TestCase("frosted")]
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
