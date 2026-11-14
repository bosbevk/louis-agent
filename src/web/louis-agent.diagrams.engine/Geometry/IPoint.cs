namespace LouisAgent.Diagrams.Engine.Geometry;

/// <summary>A place in a picture: how far across and how far down.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public interface IPoint
{
    /// <summary>How far across it is, from the picture's left.</summary>
    double X { get; }

    /// <summary>How far down it is, from the picture's top.</summary>
    double Y { get; }
}
