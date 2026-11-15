namespace LouisAgent.Web.Tests.Rules.Styles;

/// <summary>
/// F151.4: every surface in the framework's web is drawn by the shared tools, which read the names a theme sets, so a
/// theme reaches every piece. A sheet draws a surface by hand when a declaration of <c>background</c>, <c>border</c>
/// on any side, <c>box-shadow</c>, <c>backdrop-filter</c> or <c>outline</c> paints something: a colour, a name, a
/// gradient, a shadow or a blur (Louis's pick A of three, and outline with it). <c>none</c>, <c>0</c>,
/// <c>transparent</c> and <c>inherit</c> clear rather than paint, and <c>border-radius</c> is shape, so none of them
/// counts. The tools and the settings are where the drawing lives, so they are not read. The sheets that still draw
/// by hand are listed, and the list may only shrink: each family's step takes its sheets off, and the last leaves it
/// empty.
/// </summary>
public class SurfacesAreDrawnByTheToolsTests
{
    /// <summary>The framework's web, whose component and global sheets are read.</summary>
    private static readonly string Web = Path.Combine(TestPaths.RepoRoot, "src/web/louis-agent.web");

    /// <summary>The sheets that still draw a surface by hand, by their path in the framework's web.</summary>
    private static readonly string[] StillByHand =
    [
        "Components/Admin/AdminGate.razor.scss",
        "Components/Admin/AdminShell.razor.scss",
        "Components/Admin/Overview/LatestTalks.razor.scss",
        "Components/Admin/Overview/OverviewBars.razor.scss",
        "Components/Blocks/FormCard.razor.scss",
        "Components/Blocks/OfferBox.razor.scss",
        "Components/Blocks/OfferCards.razor.scss",
        "Components/BlocksView.razor.scss",
        "Components/Chat/Composer.razor.scss",
        "Components/Chat/Composing/ComposerTools.razor.scss",
        "Components/Chat/Header/ChatHeader.razor.scss",
        "Components/Chat/Header/CreditsMenu.razor.scss",
        "Components/Chat/Header/ProfileMenu.razor.scss",
        "Components/Chat/Panels/WorkItem.razor.scss",
        "Components/Chat/Panels/WorkPanel.razor.scss",
        "Components/Chat/Panels/WorkStep.razor.scss",
        "Components/Chat/Search/ChatSearch.razor.scss",
        "Components/Chat/Shell/ChatShell.razor.scss",
        "Components/Chat/Sidebar/ChatSidebar.razor.scss",
        "Components/Chat/Sidebar/LocationPicker.razor.scss",
        "Components/Chat/Toast.razor.scss",
        "Components/Chat/Turns/AnswerActions.razor.scss",
        "Components/DelegationCard.razor.scss",
        "Components/Editing/Inline/InlineEdit.razor.scss",
        "Components/Editing/Inline/InlineForm.razor.scss",
        "Components/Editing/Inline/ItemAdd.razor.scss",
        "Components/Editing/Inline/ItemTools.razor.scss",
        "Components/Editing/TagInput.razor.scss",
        "Components/Editing/TextField.razor.scss",
        "Components/Imports/ImportCard.razor.scss",
        "Components/Recorded/RecordedChat.razor.scss",
        "Components/Screens/Forms/ScreenForm.razor.scss",
        "Components/Screens/Table/ScreenTable.razor.scss",
        "Components/Site/Showroom/Mock/Foot/MockFoot.razor.scss",
        "Components/Site/Showroom/Mock/Head/MockHead.razor.scss",
        "Components/Site/Showroom/Mock/Hero/MockHero.razor.scss",
        "Components/Site/Showroom/Mock/Live/MockLiveChat.razor.scss",
        "Components/Site/Showroom/Mock/Menu/MockMenu.razor.scss",
        "Components/Site/Showroom/Mock/MockWidget.razor.scss",
        "Components/Site/Showroom/Mock/Places/MockCard.razor.scss",
        "Components/Site/Showroom/Mock/Places/MockSection.razor.scss",
        "Components/Site/Showroom/Mock/SiteMock.razor.scss",
        "Components/Site/Showroom/Mock/Website/MockPage.razor.scss",
        "Components/Site/Showroom/Shell/DemoChips.razor.scss",
        "Components/Site/Showroom/Shell/DemoRail.razor.scss",
        "Components/Site/Showroom/Shell/ShowroomShell.razor.scss",
        "Components/Site/Showroom/Stage/DemoCues.razor.scss",
        "Components/Site/Showroom/Stage/DemoNotes.razor.scss",
        "Components/Site/Showroom/Stage/DemoStage.razor.scss",
        "Components/Site/Showroom/Start/ShowroomStart.razor.scss",
        "Components/Staging/Decks/Deck.razor.scss",
        "Components/Staging/Docks/Dock.razor.scss",
        "Components/Staging/LitStage.razor.scss",
        "Components/Staging/Windows/FramedWindow.razor.scss",
        "Components/Staging/Windows/PlainBody.razor.scss",
        "Components/Staging/Windows/SiteBody.razor.scss",
        "Components/Staging/Windows/WorkBody.razor.scss",
        "Components/Widget/Bar/AgentBar.razor.scss",
        "Components/Widget/Bar/Caption/AgentCaption.razor.scss",
        "Components/Widget/Bar/Talk/AgentTalk.razor.scss",
        "Components/Widget/ChatWidget.razor.scss",
        "Components/Widget/Door/GuideButton.razor.scss",
        "Components/Widget/Orb/Orb.razor.scss",
        "Components/Widget/WindowButtons.razor.scss",
        "Pages/Admin/Packs.razor.scss",
        "Styles/web/base/_elements.scss",
        "Styles/web/chat/_bars.scss",
        "Styles/web/chat/_conversation.scss",
        "Styles/web/chat/_finish.scss",
        "Styles/web/chat/_interaction.scss",
        "Styles/web/chat/_pages.scss",
        "Styles/web/layout/_panels.scss",
        "Styles/web/layout/_responsive.scss",
        "Styles/web/layout/_shell.scss"
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

    /// <summary>Every component and global sheet of the framework's web, the tools and settings left out.</summary>
    private static IEnumerable<string> Sheets() =>
        Directory.EnumerateFiles(Web, "*.scss", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(Web, file).Replace('\\', '/'))
            .Where(file => !file.StartsWith("obj/", StringComparison.Ordinal) &&
                           !file.StartsWith("bin/", StringComparison.Ordinal) &&
                           !file.Contains("/tools/", StringComparison.Ordinal) &&
                           !file.Contains("/settings/", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal);

    /// <summary>Whether a sheet has a declaration that paints a surface.</summary>
    private static bool DrawsByHand(string sheet) =>
        File.ReadLines(Path.Combine(Web, sheet))
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

    /// <summary>Every sheet that draws a surface by hand is on the list, so a new one can't join in silence.</summary>
    [Test]
    public void EverySheetThatDrawsByHand_IsListed()
    {
        var unlisted = Sheets().Where(DrawsByHand).Except(StillByHand).ToList();

        Assert.That(unlisted, Is.Empty,
            "Draw these through the surface tool, tools/_surface.scss:\n  " + string.Join("\n  ", unlisted));
    }

    /// <summary>The surface tool's source.</summary>
    private static string Tool => File.ReadAllText(Path.Combine(Web, "Styles/web/tools/_surface.scss"));

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
    public void TheTool_DrawsEachKindOfSurface(string kind) =>
        Assert.That(Tool, Does.Contain($"@mixin {kind}("));

    /// <summary>
    /// Every drawing the tool lays on a piece is one the framework names, so it reads no name a theme does not set.
    /// </summary>
    [Test]
    public void EveryDrawingTheToolReads_IsNamedByTheFramework()
    {
        string names = File.ReadAllText(Path.Combine(Web, "Styles/web/settings/_surfaces.scss"));
        var read = Tool.Split("@include -drawn(").Skip(1).Select(call => call[..call.IndexOf(',')].Trim()).Distinct()
            .ToList();
        Assert.That(read, Has.Count.GreaterThan(10), "the tool's drawings were read");

        Assert.That(read.Where(drawing => !names.Contains($"--web-art-{drawing}:", StringComparison.Ordinal)),
            Is.Empty);
    }

    /// <summary>The list names no sheet that is clean, so it only shrinks.</summary>
    [Test]
    public void TheList_NamesNoCleanSheet()
    {
        var clean = StillByHand.Where(sheet => !File.Exists(Path.Combine(Web, sheet)) || !DrawsByHand(sheet));

        Assert.That(clean, Is.Empty, "take it off the list");
    }
}
