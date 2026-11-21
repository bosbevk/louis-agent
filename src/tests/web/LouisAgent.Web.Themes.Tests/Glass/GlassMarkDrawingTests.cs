using System.Text.RegularExpressions;

namespace LouisAgent.Web.Themes.Tests.Glass;

/// <summary>
/// FX178: inside the glass mark, coloured lights drift across the ball under one blur. That blur's region was worked
/// out from the drifting group's own box, so a light that drifted out of it was cut along a straight line, too fine to
/// see in the bar and plain on a mark drawn large (Louis, of the line through the orb: "it is there has to be fixed").
/// The blur covers the whole drawing, so nothing in the ball is ever cut.
/// </summary>
public partial class GlassMarkDrawingTests
{
    /// <summary>The themes project's glass drawings on disk.</summary>
    private static readonly string Glass =
        Path.Combine(TestPaths.RepoRoot, "src/web/LouisAgent.Web.Themes/wwwroot/glass");

    /// <summary>The filter the drifting lights wear: <c>class="fm-xbl" filter="url(#xblb5)"</c> gives <c>xblb5</c>.</summary>
    [GeneratedRegex("""class="fm-[a-z]+" filter="url\(#(?<id>[a-z0-9]+)\)""", RegexOptions.NonBacktracking)]
    private static partial Regex LightsFilter();

    /// <summary>Each mark drawing, in each style.</summary>
    private static IEnumerable<string> Marks() =>
        Directory.EnumerateFiles(Glass, "mark*.svg", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(Glass, file).Replace('\\', '/'));

    /// <summary>The blur on the drifting lights covers the whole 120 by 120 drawing, in the drawing's own measures.</summary>
    [TestCaseSource(nameof(Marks))]
    public void TheDriftingLightsBlur_CoversTheWholeDrawing(string mark)
    {
        string svg = File.ReadAllText(Path.Combine(Glass, mark));
        string id = LightsFilter().Match(svg).Groups["id"].Value;

        Assert.That(svg, Does.Contain(
            $"<filter id=\"{id}\" filterUnits=\"userSpaceOnUse\" x=\"0\" y=\"0\" width=\"120\" height=\"120\">"));
    }
}
