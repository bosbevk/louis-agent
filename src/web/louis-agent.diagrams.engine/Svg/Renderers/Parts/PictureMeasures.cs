namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts;

/// <summary>
/// The four numbers in which one kind's picture differs from another's when it is put together (CL65.2): the room
/// its title takes above the drawing, the room between the drawing and its key, and how far under that room the
/// light's middle is and how far down it reaches.
/// </summary>
/// <remarks>
/// Parameter Object (Fowler), as <c>SequenceMeasures</c> is for a sequence's layout. The usual numbers are a
/// flow's, a class chart's and a composite's. A kind that differs says where, by name, and the builder holds no
/// branch on the kind.
/// </remarks>
/// <seealso href="https://refactoring.com/catalog/introduceParameterObject.html">Introduce Parameter Object (Fowler)</seealso>
internal sealed record PictureMeasures(
    double Head = 44,
    double KeyGap = 26,
    double LightDrop = 30,
    double LightDown = 190)
{
    /// <summary>The usual numbers: a flow's, a class chart's and a composite's.</summary>
    public static readonly PictureMeasures Usual = new();
}
