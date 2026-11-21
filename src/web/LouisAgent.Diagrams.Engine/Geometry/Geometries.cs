using LouisAgent.Diagrams.Engine.Geometry.Generic;

namespace LouisAgent.Diagrams.Engine.Geometry;

/// <summary>Which geometry the engine draws with.</summary>
/// <remarks>
/// Two families are kept behind the seam, and this one line says which is in use (Louis: "keep ours and MS's but
/// use ours."): our own, in <c>double</c>. A layout handed no geometry takes this one.
/// </remarks>
public static class Geometries
{
    /// <summary>Our own family in <c>double</c>, which draws every picture.</summary>
    public static IGeometry InUse { get; } = new GenericGeometry<double>();
}
