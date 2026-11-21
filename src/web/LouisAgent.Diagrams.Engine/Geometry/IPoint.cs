namespace LouisAgent.Diagrams.Engine.Geometry;

/// <summary>A place in a picture: how far across and how far down.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public interface IPoint
{
    /// <summary>How far across it is, from the picture's left.</summary>
    double X { get; }

    /// <summary>How far down it is, from the picture's top.</summary>
    double Y { get; }

    /// <summary>The same point, somewhere else.</summary>
    IPoint Moved(double across, double down);

    /// <summary>The point turned over the picture's diagonal: across becomes down.</summary>
    IPoint Turned();

    /// <summary>
    /// How far it is to another point for a line that runs only across and down: the two distances added.
    /// </summary>
    double StepsTo(IPoint other);

    /// <summary>
    /// The point a little way off this one, towards another that is level with it or straight above or under,
    /// as a line stops short of a corner to round it.
    /// </summary>
    IPoint Toward(IPoint other, double by);
}
