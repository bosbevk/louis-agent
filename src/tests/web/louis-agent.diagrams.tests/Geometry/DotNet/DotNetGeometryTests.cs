using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Geometry.DotNet;
using LouisAgent.Diagrams.Engine.Geometry.Generic;

namespace LouisAgent.Diagrams.Tests.Geometry.DotNet;

/// <summary>
/// CL65.6: Microsoft's family is kept beside ours. What it answers differs from ours in <c>double</c>, and these
/// tests say whose doing that is: our own box run in <c>float</c> answers exactly what Microsoft's does, so the
/// whole of the difference is single precision's and none of it the library's.
/// </summary>
public class DotNetGeometryTests
{
    private static readonly IGeometry Theirs = new DotNetGeometry();
    private static readonly IGeometry OursInFloat = new GenericGeometry<float>();
    private static readonly IGeometry Ours = new GenericGeometry<double>();

    /// <summary>
    /// Two hundred boxes whose numbers have no exact form, the same ones each run: a place and a size in
    /// tenths and hundredths, as a layout's sums leave them.
    /// </summary>
    private static IEnumerable<(double X, double Y, double W, double H)> Awkward()
    {
        var random = new Random(65);
        for (int at = 0; at < 200; at++)
            yield return (random.Next(0, 20000) / 10.0, random.Next(0, 20000) / 10.0,
                random.Next(1, 40000) / 100.0, random.Next(1, 40000) / 100.0);
    }

    /// <summary>Everything a box answers, as numbers to hold one family's against another's.</summary>
    private static double[] Answers(IGeometry geometry, (double X, double Y, double W, double H) made)
    {
        var box = geometry.Box(made.X, made.Y, made.W, made.H);
        IBox grown = box.Grown(7.3), moved = box.Moved(0.7, -1.9), joined = box.With(geometry.Box(1.1, 2.2, 3.3, 4.4));

        return
        [
            box.Left, box.Top, box.Width, box.Height, box.Right, box.Bottom, box.Middle.X, box.Middle.Y,
            grown.Left, grown.Top, grown.Width, grown.Height, moved.Left, moved.Top,
            joined.Left, joined.Top, joined.Width, joined.Height
        ];
    }

    /// <summary>Our box in single precision answers exactly what Microsoft's does, for every sum a box has.</summary>
    [Test]
    public void OursInFloat_AnswersExactlyWhatMicrosoftsDoes()
    {
        var differing = Awkward().Where(made => !Answers(Theirs, made).SequenceEqual(Answers(OursInFloat, made)));

        Assert.That(differing, Is.Empty, "a box where the library, not the precision, changed a number");
    }

    /// <summary>
    /// Against ours in <c>double</c>, Microsoft's differs for most of those boxes, and by no more than single
    /// precision can: under a thousandth of a pixel at these sizes, which an SVG written to one decimal hides.
    /// </summary>
    [Test]
    public void AgainstOursInDouble_MicrosoftsDiffersBySinglePrecisionOnly()
    {
        var gaps = Awkward().Select(made => Answers(Theirs, made).Zip(Answers(Ours, made))
            .Max(pair => Math.Abs(pair.First - pair.Second))).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(gaps.Count(gap => gap > 0), Is.GreaterThan(150), "most boxes differ in their last digits");
            Assert.That(gaps.Max(), Is.LessThan(0.001));
        });
    }
}
