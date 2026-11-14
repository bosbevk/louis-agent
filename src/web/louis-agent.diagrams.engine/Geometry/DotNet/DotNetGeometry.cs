using System.Drawing;
using System.Numerics;

namespace LouisAgent.Diagrams.Engine.Geometry.DotNet;

/// <summary>
/// Makes boxes and points of Microsoft's family, on <see cref="RectangleF"/> and <see cref="Vector2"/>.
/// </summary>
/// <remarks>
/// The factory of the second family (Abstract Factory). It is kept and run by a test, and the engine draws with
/// our own. A number handed in is cut to single precision here, which is all that tells this family's answers
/// from ours.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public sealed class DotNetGeometry : IGeometry
{
    /// <summary>A box of Microsoft's.</summary>
    public IBox Box(double x, double y, double width, double height) =>
        new DotNetBox(new RectangleF((float)x, (float)y, (float)width, (float)height));

    /// <summary>A point of Microsoft's.</summary>
    public IPoint Point(double x, double y) => new DotNetPoint(new Vector2((float)x, (float)y));

    /// <summary>The smallest box of Microsoft's that holds every one of these, by its own <c>Union</c>.</summary>
    public IBox Around(IEnumerable<IBox> boxes) => new DotNetBox(boxes
        .Select(box => ((DotNetBox)Box(box.Left, box.Top, box.Width, box.Height)).Rectangle)
        .Aggregate(RectangleF.Union));
}
