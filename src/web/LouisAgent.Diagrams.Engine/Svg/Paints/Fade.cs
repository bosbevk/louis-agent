namespace LouisAgent.Diagrams.Engine.Svg.Paints;

/// <summary>
/// A fade from one colour into another, which a style fills a shape with by its id: the way it runs over the
/// shape, and where along that run each of its stops stands, from 0 to 1.
/// </summary>
/// <remarks>
/// Value Object (Fowler): a fade is named once, beside the style that colours its stops, and a drawing asks the
/// writer for it by that name. The look's glass and its accent were written out in five drawings, each with
/// the fade's id, its run and the same stops, so they agreed only while the copies did.
/// </remarks>
/// <param name="Id">The id a style fills with, <c>url(#id)</c>, and which classes each stop: <c>id-0</c>.</param>
/// <param name="Run">Which way it runs over the shape it fills.</param>
/// <param name="Stops">Where each stop stands along the run.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
internal sealed record Fade(string Id, PaintRun Run, params double[] Stops);
