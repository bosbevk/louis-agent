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
}
