using System.Numerics;

namespace LouisAgent.Diagrams.Engine.Geometry.DotNet;

/// <summary>A point of Microsoft's family: a <see cref="Vector2"/>, read through the seam.</summary>
/// <seealso href="https://learn.microsoft.com/dotnet/api/system.numerics.vector2">Vector2 (Microsoft Learn)</seealso>
public sealed record DotNetPoint(Vector2 Vector) : IPoint
{
    /// <summary>How far across.</summary>
    public double X => Vector.X;

    /// <summary>How far down.</summary>
    public double Y => Vector.Y;

    /// <summary>The same point, somewhere else.</summary>
    public IPoint Moved(double across, double down) =>
        new DotNetPoint(Vector + new Vector2((float)across, (float)down));

    /// <summary>The point turned over the picture's diagonal.</summary>
    public IPoint Turned() => new DotNetPoint(new Vector2(Vector.Y, Vector.X));

    /// <summary>How far it is to another point for a line that runs only across and down.</summary>
    public double StepsTo(IPoint other)
    {
        var apart = Vector2.Abs(Theirs(other) - Vector);
        return apart.X + apart.Y;
    }

    /// <summary>
    /// The point a little way off this one, towards another level with it or straight above or under.
    /// </summary>
    public IPoint Toward(IPoint other, double by)
    {
        var way = Theirs(other) - Vector;
        return new DotNetPoint(Vector + new Vector2(MathF.Sign(way.X), MathF.Sign(way.Y)) * (float)by);
    }

    /// <summary>Another point as a vector: its own when it is of this family, read through the seam when not.</summary>
    private static Vector2 Theirs(IPoint point) =>
        point is DotNetPoint ours ? ours.Vector : new Vector2((float)point.X, (float)point.Y);
}
