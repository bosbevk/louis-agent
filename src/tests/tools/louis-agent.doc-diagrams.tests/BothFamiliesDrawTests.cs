using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Geometry.DotNet;
using LouisAgent.Diagrams.Engine.Geometry.Generic;
using LouisAgent.Diagrams.Engine.Svg;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.DocDiagrams.Tests;

/// <summary>
/// CL65.6: the engine has two families of boxes and points behind one seam. Ours, in <c>double</c>, is the one in
/// use, and Microsoft's, on <c>RectangleF</c> and <c>Vector2</c>, is kept beside it (Louis: "keep ours and MS's
/// but use ours."). So every picture of the docs is drawn with Microsoft's too, and held against ours.
/// </summary>
/// <remarks>
/// What the two draw differently is single precision's doing and none of the family's: our own family run in
/// <c>float</c> draws what Microsoft's draws, to the byte. Beside ours in <c>double</c> most pictures are the
/// same or have a few numbers a tenth of a pixel apart, and in a few flows an arrow takes another way. Those are
/// counted and written out when the tests run, since the docs' pictures change with every plan.
/// </remarks>
public partial class BothFamiliesDrawTests
{
    /// <summary>
    /// How far apart two written numbers may be and still be the same to a tenth: one step of the one decimal a
    /// picture writes, with a little room for the subtraction's own rounding.
    /// </summary>
    private const double ATenth = 0.11;

    /// <summary>The part of the pictures that must be the same to a tenth, whichever family draws them.</summary>
    private const double NineInTen = 0.9;

    /// <summary>
    /// A number as a picture writes it. It finds <c>36.9</c>, <c>-24</c> and <c>0</c>, and nothing in
    /// <c>seq-life</c>.
    /// </summary>
    [GeneratedRegex(@"-?\d+(?:\.\d+)?")]
    private static partial Regex Number();

    /// <summary>One picture of the docs, drawn three ways.</summary>
    /// <param name="Name">Its file's name.</param>
    /// <param name="Kind">Its kind.</param>
    /// <param name="Ours">Drawn by the geometry in use.</param>
    /// <param name="Microsofts">Drawn by Microsoft's family.</param>
    /// <param name="OursInSingle">Drawn by our own family in single precision.</param>
    private sealed record Drawn(string Name, DiagramKind Kind, string Ours, string Microsofts, string OursInSingle);

    /// <summary>Every picture of the docs, drawn once for all the tests here.</summary>
    private static readonly Lazy<IReadOnlyList<Drawn>> Pictures = new(() =>
        [.. DiagramSync.Find(DiagramSync.Roots(TestPaths.RepoRoot)).Select(Draw)]);

    /// <summary>
    /// A picture drawn by each geometry, as the docs' tool draws it. A refusal fails the test by name.
    /// </summary>
    private static Drawn Draw(string json)
    {
        var source = DiagramReader.Read(File.ReadAllText(json));
        string By(IGeometry geometry)
        {
            try
            {
                return DiagramRenderers.RenderTitled(source, geometry);
            }
            catch (Exception refusal)
            {
                throw new AssertionException($"{Path.GetFileName(json)} is not drawn by {geometry.GetType().Name}",
                    refusal);
            }
        }

        return new Drawn(Path.GetFileName(json), source.Kind, By(Geometries.InUse), By(new DotNetGeometry()),
            By(new GenericGeometry<float>()));
    }

    /// <summary>
    /// How far apart two drawings of a picture are: the furthest any number of one stands from the same number
    /// of the other. Endlessly far when they differ in more than their numbers.
    /// </summary>
    private static double Apart(string one, string other)
    {
        if (Number().Replace(one, "#") != Number().Replace(other, "#")) return double.PositiveInfinity;

        return Number().Matches(one).Zip(Number().Matches(other), (a, b) => Math.Abs(Written(a) - Written(b)))
            .DefaultIfEmpty(0).Max();
    }

    /// <summary>A number a picture wrote, read back.</summary>
    private static double Written(Match number) => double.Parse(number.Value, CultureInfo.InvariantCulture);

    /// <summary>A drawing's shapes, each by what it is and its class, and its words, in the order drawn.</summary>
    private static (List<string> Shapes, List<string> Words) Parts(string svg)
    {
        var all = XElement.Parse(svg).Descendants().ToList();
        return ([.. all.Select(shape => $"{shape.Name.LocalName}.{(string?)shape.Attribute("class")}")],
            [.. all.Where(shape => shape.Name.LocalName == "text").Select(shape => shape.Value)]);
    }

    /// <summary>Whether two drawings have the same shapes in the same order, and the same words.</summary>
    private static bool SameParts(string one, string other)
    {
        var (shapes, words) = Parts(one);
        var (otherShapes, otherWords) = Parts(other);
        return shapes.SequenceEqual(otherShapes) && words.SequenceEqual(otherWords);
    }

    /// <summary>
    /// Microsoft's family draws every picture of the docs, none refused, each with the shapes ours has, in the
    /// same order, and the same words.
    /// </summary>
    [Test]
    public void MicrosoftsFamily_DrawsEveryPicture_WithOursShapesAndWords()
    {
        var other = Pictures.Value.Where(picture => !SameParts(picture.Ours, picture.Microsofts))
            .Select(picture => picture.Name);

        Assert.Multiple(() =>
        {
            Assert.That(Pictures.Value, Has.Count.GreaterThan(200), "the docs' pictures");
            Assert.That(other, Is.Empty, "a picture Microsoft's family draws with other shapes or other words");
        });
    }

    /// <summary>
    /// What Microsoft's family draws differently from ours is single precision's doing, and none of the family's:
    /// our own family, run in <c>float</c>, draws every picture exactly as Microsoft's does.
    /// </summary>
    [Test]
    public void OurFamilyInSinglePrecision_DrawsExactlyWhatMicrosoftsDoes()
    {
        var differing = Pictures.Value.Where(picture => picture.OursInSingle != picture.Microsofts)
            .Select(picture => picture.Name);

        Assert.That(differing, Is.Empty,
            "a picture the two families draw differently in the same precision: one of them does a sum another way");
    }

    /// <summary>
    /// Beside ours in <c>double</c>, at least nine pictures in ten are the same to a tenth of a pixel. The rest
    /// are flows where an arrow takes another way. How many are the same, a tenth apart and further apart is
    /// written out, with the names of the last.
    /// </summary>
    [Test]
    public void MostPictures_AreTheSameToATenth_WhicheverFamilyDrawsThem()
    {
        var apart = Pictures.Value.Select(picture => (Picture: picture, By: Apart(picture.Ours, picture.Microsofts)))
            .ToList();
        var further = apart.Where(one => one.By >= ATenth).ToList();
        int same = apart.Count(one => one.By == 0), near = apart.Count - same - further.Count;

        TestContext.Out.WriteLine($"{apart.Count} pictures: {same} the same, {near} with numbers a tenth apart, " +
                                  $"{further.Count} further apart.");
        foreach (var (picture, _) in further)
            TestContext.Out.WriteLine($"Further apart: {picture.Kind} {picture.Name}");

        Assert.That(same + near, Is.GreaterThanOrEqualTo(NineInTen * apart.Count),
            "Microsoft's family draws too many pictures another way: " +
            string.Join(", ", further.Select(one => one.Picture.Name)));
    }
}
