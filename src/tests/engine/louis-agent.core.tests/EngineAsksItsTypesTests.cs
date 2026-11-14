using System.Text.RegularExpressions;

namespace LouisAgent.Core.Tests;

/// <summary>
/// The diagram engine asks its types (CL65.6, ADR 0123, Louis: "we cannot write spaghetti code"): a box knows its
/// edges, a face how wide words are, and a path writer a path's text, so no layout or drawing works those out
/// where it stands. And a picture's geometry is chosen in one place.
/// </summary>
/// <remarks>
/// Each test reads the engine's source for one shape that says a type was not asked, outside the folders of the
/// types themselves, and fails for any file that holds it. They were ratchets while the kinds moved onto the
/// types, each with a list that could only shrink. The lists are empty, so a shape is refused wherever it turns
/// up.
/// </remarks>
public partial class EngineAsksItsTypesTests
{
    private const string Engine = "src/web/louis-agent.diagrams.engine";

    /// <summary>What to do about a file a test names.</summary>
    private const string Hint = "each file listed works out what a type could answer: ask the type (ADR 0123).";

    /// <summary>The folders of the types themselves, where the sums are meant to be.</summary>
    private static readonly string[] TheTypes = ["/Geometry/", "/Words/", "/Svg/Paths/"];

    /// <summary>
    /// A record that is a box by four numbers. It finds "double X, double Y, double W, double H" and the same
    /// with <c>Width</c> and <c>Height</c>, over several lines too, and nothing in "double X, double Y, double H".
    /// </summary>
    [GeneratedRegex(@"double X,\s*double Y,\s*double (?:W|Width),\s*double (?:H|Height)\b",
        RegexOptions.NonBacktracking)]
    private static partial Regex BoxByNumbers();

    /// <summary>
    /// A box's edge or half its size, worked out. It finds "node.X + node.W", "box.Top + box.Height" and
    /// "node.W / 2", and nothing in "x + 4" or "width / 2", where no box is named.
    /// </summary>
    [GeneratedRegex(@"\.(?:X|Left) \+ [\w.]*\.?(?:W|Width)\b|\.(?:Y|Top) \+ [\w.]*\.?(?:H|Height)\b" +
                    @"|\.(?:W|H|Width|Height) / 2\b", RegexOptions.NonBacktracking)]
    private static partial Regex BoxSum();

    /// <summary>
    /// Words measured by their length. It finds "name.Length * NameChar" and "Max(line => line.Length) * Char",
    /// and nothing in "lines.Count * LineStep".
    /// </summary>
    [GeneratedRegex(@"\.Length\)? \* ", RegexOptions.NonBacktracking)]
    private static partial Regex WordsByLength();

    /// <summary>
    /// A path built as text. It finds <c>$"M{N(x)} …"</c>, <c>$"M {N(x)} …"</c>, <c>$"M0 {N(top)}"</c> and
    /// <c>"M" + string.Join(…)</c>, and nothing in <c>"Marquee"</c>.
    /// </summary>
    [GeneratedRegex("""\$"M ?[{\d]|"M" \+""", RegexOptions.NonBacktracking)]
    private static partial Regex PathAsText();

    /// <summary>The engine's files that hold a shape, outside the types' own folders, by path under it.</summary>
    private static List<string> Holding(Regex shape) =>
    [
        .. ProjectSources.Of(Engine).Where(source => !TheTypes.Any(source.Path.Contains))
            .Where(source => shape.IsMatch(source.Text))
            .Select(source => source.Path[(Engine.Length + 1)..]).Order(StringComparer.Ordinal)
    ];

    /// <summary>A place and a size are a box, never four numbers side by side.</summary>
    [Test]
    public void NoRecord_IsABoxByFourNumbers() => Assert.That(Holding(BoxByNumbers()), Is.Empty, Hint);

    /// <summary>A box is asked for its edge and its middle.</summary>
    [Test]
    public void NoFile_WorksOutABoxsEdge() => Assert.That(Holding(BoxSum()), Is.Empty, Hint);

    /// <summary>A face is asked how wide words are.</summary>
    [Test]
    public void NoFile_MeasuresWordsByTheirLength() => Assert.That(Holding(WordsByLength()), Is.Empty, Hint);

    /// <summary>A path writer writes a path.</summary>
    [Test]
    public void NoFile_BuildsAPathAsText() => Assert.That(Holding(PathAsText()), Is.Empty, Hint);

    /// <summary>
    /// Which geometry draws a picture is said once, by the list of renderers, and a layout names the one in use
    /// only as what it falls back to when it is told none. No renderer and no drawing picks one for itself.
    /// </summary>
    [Test]
    public void NoDrawing_PicksItsOwnGeometry()
    {
        var naming = ProjectSources.Of(Engine).Where(source => source.Text.Contains("Geometries.InUse"))
            .Select(source => source.Path[(Engine.Length + 1)..]).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(naming.Where(path => path.StartsWith("Svg/", StringComparison.Ordinal)),
                Is.EqualTo(new[] { "Svg/DiagramRenderers.cs" }), "among the drawings, only the list of renderers");
            Assert.That(naming, Has.Count.GreaterThan(1), "and the layouts, as what they fall back to");
        });
    }
}
