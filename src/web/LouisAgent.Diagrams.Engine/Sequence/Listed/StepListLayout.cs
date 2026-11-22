using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Sequence.Listed.Placed;
using LouisAgent.Diagrams.Sequence.Spec;

namespace LouisAgent.Diagrams.Engine.Sequence.Listed;

/// <summary>
/// Lays a sequence out as steps down a page of a given width, for a phone: each call and answer with its two ends
/// and its words, each note on its glass, and each block a frame round its own steps, a level in
/// (<see cref="StepListRows"/>).
/// </summary>
/// <remarks>
/// It decides every place and draws nothing, as <see cref="SequenceLayout"/> does for the wide drawing, so it is a
/// pure function of the data and is tested with numbers. Its numbers are the <see cref="StepListMeasures"/> it is
/// handed.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0118-a-diagram-is-data-our-renderer-draws.md">ADR 0118: a diagram is data our renderer draws</seealso>
public static class StepListLayout
{
    /// <summary>Where every step and frame stands, laid out to the width it is handed, by the measures handed.</summary>
    public static PlacedStepList Place(SequenceSpec spec, double width, StepListMeasures measures,
        IGeometry? geometry = null)
    {
        var people = spec.Participants.ToDictionary(participant => participant.Id, StringComparer.Ordinal);
        var rows = new StepListRows(people, spec.Numbered, measures, geometry ?? Geometries.InUse);
        double height = rows.Walk(spec.Steps, 0, 0, width, 0);
        return new PlacedStepList(rows.Steps, rows.Blocks, width, height);
    }
}
