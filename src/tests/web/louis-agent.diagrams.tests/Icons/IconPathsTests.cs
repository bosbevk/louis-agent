using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Svg;
using LouisAgent.Diagrams.Svg.Theme;

namespace LouisAgent.Diagrams.Tests.Icons;

/// <summary>
/// The icons a card wears are Tabler Icons (MIT, Louis's pick P3), their path data copied in, with no package or
/// font: every icon has its paths, the licence travels with them, and the writer draws one as a stroked line drawing
/// in Tabler's 24-unit box, scaled to the size asked.
/// </summary>
public class IconPathsTests
{
    /// <summary>The repo's root, found upward from the test's build output.</summary>
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(dir.FullName, "louis-agent-solution.sln"))) dir = dir.Parent!;
        return dir.FullName;
    }

    /// <summary>Every icon a file may name has its paths, each a path's data.</summary>
    [Test]
    public void EveryIcon_HasItsPaths() =>
        Assert.Multiple(() =>
        {
            foreach (var icon in Enum.GetValues<DiagramIcon>())
                Assert.That(IconPaths.Of(icon), Is.Not.Empty.And.All.StartsWith("M"), Enum.GetName(icon));
        });

    /// <summary>Tabler's MIT notice sits beside the icons, and the notices file names it.</summary>
    [Test]
    public void TheLicence_TravelsWithTheIcons()
    {
        string licence = Path.Combine(RepoRoot(), "src/web/louis-agent.diagrams/Icons/LICENSE-tabler-icons.txt");

        Assert.Multiple(() =>
        {
            Assert.That(File.ReadAllText(licence), Does.Contain("MIT License").And.Contain("Paweł Kuna"));
            Assert.That(File.ReadAllText(Path.Combine(RepoRoot(), "THIRD-PARTY-NOTICES.md")),
                Does.Contain("Tabler Icons"));
        });
    }

    /// <summary>An icon is a group of stroked paths, moved to its place and scaled from Tabler's 24-unit box.</summary>
    [Test]
    public void AnIcon_IsDrawnAsStrokedPaths_ScaledFromItsBox()
    {
        string svg = new SvgWriter().Icon(10, 20, 18, DiagramIcon.Chip, "icon").Write(SvgTheme.Site, "An icon", 40, 40);

        Assert.Multiple(() =>
        {
            Assert.That(svg, Does.Contain("<g class=\"icon\" transform=\"translate(10 20) scale(0.75)\">"));
            Assert.That(svg, Does.Contain($"d=\"{IconPaths.Of(DiagramIcon.Chip)[0]}\""));
        });
    }
}
