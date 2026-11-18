using LouisAgent.Diagrams.Engine.Svg;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Tests.Svg.Theme;

/// <summary>
/// Every picture is written in the site's fonts (F162.1, Louis: "all diagrams must use font from website", "it is
/// like the framework defaults"): on a page it reads the site's own font settings, and drawn as a file it falls back
/// to the framework's defaults, Geist and Geist Mono, the very lists the framework's web sets.
/// </summary>
public class PictureFontsTests
{
    /// <summary>The repo's root, found upward from the test's build output.</summary>
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(dir.FullName, "louis-agent-solution.sln"))) dir = dir.Parent!;
        return dir.FullName;
    }

    /// <summary>What the framework's web sets a font setting to, read from its own Sass.</summary>
    private static string Framework(string setting)
    {
        string colours = Path.Combine(RepoRoot(), "src/web/louis-agent.web/Styles/web/settings/_colours.scss");
        string line = File.ReadLines(colours).First(line => line.TrimStart().StartsWith($"--{setting}:"));
        return line[(line.IndexOf(':') + 1)..].Trim().TrimEnd(';');
    }

    /// <summary>A small picture, as a docs file draws it.</summary>
    private static string Drawn() => DiagramRenderers.Render(DiagramReader.Read("""
        { "kind": "flow", "type": "activity", "title": "Two steps", "flow": { "nodes": [ { "id": "a", "label": "A" },
          { "id": "b", "label": "B" } ], "edges": [ { "from": "a", "to": "b" } ] } }
        """));

    /// <summary>The words read the site's own sans and mono, and fall back to the framework's own lists.</summary>
    [TestCase("pic-sans", "bs-font-sans-serif")]
    [TestCase("mono", "bs-font-monospace")]
    public void APicturesFont_IsTheSites_ElseTheFrameworksDefault(string picture, string site) =>
        Assert.That(Drawn(), Does.Contain($"--{picture}: var(--{site}, {Framework(site)});"));
}
