using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Sequence.Listed.Placed;
using LouisAgent.Diagrams.Sequence.Spec;
using LouisAgent.Diagrams.Sequence.Spec.Steps;

namespace LouisAgent.Diagrams.Engine.Sequence.Listed;

/// <summary>
/// Lays a sequence out as steps down a page of a given width, for a phone: each call and answer with its two ends
/// and its words, each note on its glass.
/// </summary>
/// <remarks>
/// It decides every place and draws nothing, as <see cref="SequenceLayout"/> does for the wide drawing, so it is a
/// pure function of the data and is tested with numbers. Its numbers are the <see cref="StepListMeasures"/> it is
/// handed.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0118-a-diagram-is-data-our-renderer-draws.md">ADR 0118: a diagram is data our renderer draws</seealso>
public static class StepListLayout
{
    /// <summary>Where every step stands, laid out to the width it is handed, by the measures it is handed.</summary>
    public static PlacedStepList Place(SequenceSpec spec, double width, StepListMeasures measures,
        IGeometry? geometry = null)
    {
        geometry ??= Geometries.InUse;
        var people = spec.Participants.ToDictionary(participant => participant.Id, StringComparer.Ordinal);
        var steps = new List<PlacedStep>();
        int count = 0;

        // Each step under the one before it, the gap between; the list ends at the last one's foot.
        double y = 0;
        foreach (var step in spec.Steps.Where(step => step is not BlockStep))
        {
            int? number = spec.Numbered && step is not NoteStep ? ++count : null;
            var placed = Leaf(step, people, number, 0, y, width, measures, geometry);
            steps.Add(placed);
            y = placed.Box.Bottom + measures.StepGap;
        }

        double height = steps.Count == 0 ? 0 : steps[^1].Box.Bottom;
        return new PlacedStepList(steps, [], width, height);
    }

    /// <summary>
    /// A call, an answer or a note, at a height and in a room: its ends on one line when they fit beside its number,
    /// and its words wrapped to what the bar leaves.
    /// </summary>
    /// <remarks>A switch on the step's kind, one arm a kind (ADR 0051).</remarks>
    private static PlacedStep Leaf(SequenceStep step, IReadOnlyDictionary<string, Participant> people, int? number,
        double left, double y, double room, StepListMeasures measures, IGeometry geometry)
    {
        if (step is NoteStep note)
        {
            var noteLines = measures.NoteLines(note.Text, room);
            var glass = geometry.Box(left, y, measures.NoteWidth(noteLines, room), measures.NoteHeight(noteLines));
            return new PlacedStep(step, null, noteLines, glass);
        }

        var (from, to, text) = step switch
        {
            MessageStep message => (message.From, message.To, message.Text),
            ReplyStep reply => (reply.From, reply.To, reply.Text),
            _ => throw new NotSupportedException(step.GetType().Name)
        };

        // The ends share their line with the number's ring, when there is one.
        double words = room - measures.Rail;
        double endsRoom = number is null ? words : words - measures.Ring;
        var (sender, receiver) = (people[from], people[to]);
        bool wrapped = from != to && measures.EndsWidth(sender, receiver) > endsRoom;
        var lines = measures.Lines(text, words);
        var box = geometry.Box(left, y, room, measures.StepHeight(wrapped, lines));
        return new PlacedStep(step, new PlacedEnds(sender, receiver, wrapped), lines, box, number);
    }
}
