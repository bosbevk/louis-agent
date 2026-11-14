using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Diagrams.Cards;
using LouisAgent.Diagrams.Classes.Spec;
using LouisAgent.Diagrams.Components;
using LouisAgent.Diagrams.Composite;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Messages;
using LouisAgent.Diagrams.Messages.Holes.Reading;
using LouisAgent.Diagrams.Messages.Holes.Shapes;
using LouisAgent.Diagrams.Messages.Holes.Strays;
using LouisAgent.Diagrams.Sequence.Spec;
using LouisAgent.Diagrams.Sequence.Spec.Steps;
using LouisAgent.Diagrams.Sources.Types;
using LouisAgent.Diagrams.Tickets.Spec;

namespace LouisAgent.Diagrams.Sources;

/// <summary>Reads a diagram's JSON file into its records, and refuses one it can't draw, saying why.</summary>
/// <remarks>
/// The file is a semantic model (Fowler, <i>Domain-Specific Languages</i>): plain data a person writes, read once into
/// records the renderer interprets. Everything that would make a broken picture is refused here, by name (an unknown
/// kind, a missing part, an arrow or a link to an id nothing has, a misspelt field), so the renderer never has to
/// guess and a typo never becomes a box without its arrow.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0119-declare-name-then-pass.md">ADR 0119: named types, declared, then passed</seealso>
public static class DiagramReader
{
    /// <summary>The diagram the JSON describes.</summary>
    public static DiagramSource Read(string json)
    {
        var kind = KindOf(json);
        var source = Deserialize(json);
        if (string.IsNullOrWhiteSpace(source.Title)) throw Refused(DiagramMessage.MissingTitle);
        CheckPart(kind, source);

        // Its type is its kind's own when it wrote none, and is refused when it is not one its kind may be.
        source = source with { Type = DiagramTypeCheck.Of(kind, source.Type) };
        if (source.Flow is { } flow) CheckFlow(flow);
        if (source.Cards is { } cards) CheckCards(cards);
        if (source.Sequence is { } sequence) CheckSequence(sequence);
        if (source.Classes is { } classes) CheckClasses(classes);
        if (source.Ticket is { } ticket) CheckTicket(ticket);
        if (source.Components is { } components) ComponentsCheck.Check(components);
        if (source.Composite is { } composite) CompositeCheck.Check(composite);
        return source;
    }

    /// <summary>The kind the file names, read first, so an unknown one is refused naming the kinds there are.</summary>
    private static DiagramKind KindOf(string json)
    {
        string? text;
        try
        {
            using var document = JsonDocument.Parse(json);
            text = document.RootElement.TryGetProperty("kind", out var kind) ? kind.GetString() : null;
        }
        catch (JsonException ex)
        {
            var unreadable = new UnreadableHoles(ex.Message);
            throw Refused(DiagramMessage.Unreadable, unreadable);
        }

        if (SettingChoice.TryParse<DiagramKind>(text, out var parsed)) return parsed!.Value;

        var unknown = new UnknownKindHoles(text, string.Join(", ", SettingChoice.Names<DiagramKind>()));
        throw Refused(DiagramMessage.UnknownKind, unknown);
    }

    /// <summary>
    /// The whole file as its records; a misspelt field, or a null where one isn't allowed, is refused with what the
    /// reader saw.
    /// </summary>
    private static DiagramSource Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<DiagramSource>(json, DiagramJson.Options)!;
        }
        catch (JsonException ex)
        {
            var unreadable = new UnreadableHoles(ex.Message);
            throw Refused(DiagramMessage.Unreadable, unreadable);
        }
        // A sequence's step without its "step" field can't be told apart: System.Text.Json says so this way.
        catch (NotSupportedException ex)
        {
            var unreadable = new UnreadableHoles(ex.Message);
            throw Refused(DiagramMessage.Unreadable, unreadable);
        }
    }

    /// <summary>A kind's own part must be there: a flow its boxes and arrows, cards their rows.</summary>
    private static void CheckPart(DiagramKind kind, DiagramSource source)
    {
        bool hasPart = kind switch
        {
            DiagramKind.Flow => source.Flow is not null,
            DiagramKind.Cards => source.Cards is not null,
            DiagramKind.Sequence => source.Sequence is not null,
            DiagramKind.Classes => source.Classes is not null,
            DiagramKind.Ticket => source.Ticket is not null,
            DiagramKind.Components => source.Components is not null,
            DiagramKind.Composite => source.Composite is not null,
            _ => false
        };
        if (hasPart) return;

        var missing = new MissingPartHoles(kind.Wire());
        throw Refused(DiagramMessage.MissingPart, missing);
    }

    /// <summary>Every box's group is one the flow declares, and every arrow joins two boxes the flow has.</summary>
    private static void CheckFlow(GraphSpec flow)
    {
        // A box's group must be one the diagram declares, so its frame has its label.
        var groups = (flow.Groups ?? []).Select(group => group.Id).ToHashSet(StringComparer.Ordinal);
        var astray = flow.Nodes.FirstOrDefault(node => node.Group is { } group && !groups.Contains(group));
        if (astray is not null)
        {
            var noGroup = new UnknownGroupHoles(astray.Id, astray.Group);
            throw Refused(DiagramMessage.UnknownGroup, noGroup);
        }

        var ids = flow.Nodes.Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
        var stray = flow.Edges.FirstOrDefault(edge => !ids.Contains(edge.From) || !ids.Contains(edge.To));
        if (stray is null) return;

        var noBox = new UnknownBoxHoles(stray.From, stray.To, StrayEnd(ids, stray.From, stray.To));
        throw Refused(DiagramMessage.UnknownBox, noBox);
    }

    /// <summary>
    /// Every link joins two cards the overview has, one in the row just above or just under the other: a link is
    /// drawn on a rail in the gap between two rows (F134.9), so one that skips a row or stays in one can't be drawn.
    /// </summary>
    private static void CheckCards(CardsSpec cards)
    {
        // Each card's row, counted from the top.
        var rowOf = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int row = 0; row < cards.Rows.Count; row++)
            foreach (var card in cards.Rows[row].Cards)
                rowOf[card.Id] = row;

        if (cards.Links.FirstOrDefault(link => !rowOf.ContainsKey(link.From) || !rowOf.ContainsKey(link.To))
            is { } stray)
        {
            var ids = rowOf.Keys.ToHashSet(StringComparer.Ordinal);
            var noCard = new UnknownCardHoles(stray.From, stray.To, StrayEnd(ids, stray.From, stray.To));
            throw Refused(DiagramMessage.UnknownCard, noCard);
        }

        if (cards.Links.FirstOrDefault(link => Math.Abs(rowOf[link.From] - rowOf[link.To]) != 1) is not { } far)
            return;

        var notBetween = new LinkNotBetweenRowsHoles(far.From, far.To);
        throw Refused(DiagramMessage.LinkNotBetweenRows, notBetween);
    }

    /// <summary>
    /// A sequence has a step at least, a note stands over someone, and every step names participants the chart has.
    /// </summary>
    private static void CheckSequence(SequenceSpec sequence)
    {
        if (sequence.Steps.Count == 0) throw Refused(DiagramMessage.NoSteps);
        var all = sequence.Steps.SelectMany(step => step.All()).ToList();
        if (all.OfType<NoteStep>().FirstOrDefault(note => note.Over.Count == 0) is { } bare)
        {
            var overNoOne = new NoteOverNoOneHoles(bare.Text);
            throw Refused(DiagramMessage.NoteOverNoOne, overNoOne);
        }

        if (all.OfType<BlockStep>().FirstOrDefault(Empty) is { } empty)
        {
            var emptyBlock = new EmptyBlockHoles(empty.Block.Wire(), empty.Label);
            throw Refused(DiagramMessage.EmptyBlock, emptyBlock);
        }

        var ids = sequence.Participants.Select(head => head.Id).ToHashSet(StringComparer.Ordinal);
        var stray = sequence.Steps
            .SelectMany(step => step.Names().Where(id => !ids.Contains(id)).Select(id => (Step: step, Id: id)))
            .FirstOrDefault();
        if (stray == default) return;

        var noParticipant = new UnknownParticipantHoles(stray.Id, stray.Step.ToString()!);
        throw Refused(DiagramMessage.UnknownParticipant, noParticipant);
    }

    /// <summary>Every relation joins two types the class chart has.</summary>
    private static void CheckClasses(ClassesSpec classes)
    {
        var ids = classes.Types.Select(type => type.Id).ToHashSet(StringComparer.Ordinal);
        var stray = classes.Relations.FirstOrDefault(relation =>
            !ids.Contains(relation.From) || !ids.Contains(relation.To));
        if (stray is null) return;

        var noType = new UnknownTypeHoles(stray.From, stray.To, StrayEnd(ids, stray.From, stray.To));
        throw Refused(DiagramMessage.UnknownType, noType);
    }

    /// <summary>
    /// A ticket has its headline, one to six rows (more don't fit a chat's card), and each row both its label and its
    /// value, so an agent that wrote a half-empty card hears which part to fix.
    /// </summary>
    private static void CheckTicket(TicketSpec ticket)
    {
        if (string.IsNullOrWhiteSpace(ticket.Title)) throw Refused(DiagramMessage.TicketWithoutTitle);
        if (ticket.Rows.Count is < 1 or > 6)
        {
            var rowCount = new TicketRowCountHoles(ticket.Rows.Count);
            throw Refused(DiagramMessage.TicketRowCount, rowCount);
        }

        int half = ticket.Rows.ToList()
            .FindIndex(row => string.IsNullOrWhiteSpace(row.Label) || string.IsNullOrWhiteSpace(row.Value));
        if (half < 0) return;

        var halfRow = new TicketHalfRowHoles(half + 1);
        throw Refused(DiagramMessage.TicketHalfRow, halfRow);
    }

    /// <summary>
    /// Of a line's two ends, the one nothing in the diagram has: the far end when the near one is known.
    /// </summary>
    private static string StrayEnd(HashSet<string> ids, string from, string to) => ids.Contains(from) ? to : from;

    /// <summary>A block, or one of its cases, with nothing in it: a frame round nothing.</summary>
    private static bool Empty(BlockStep block) =>
        block.Steps.Count == 0 || (block.Else ?? []).Any(@case => @case.Steps.Count == 0);

    /// <summary>The refusal, its text the message's own, which has no holes.</summary>
    private static InvalidDataException Refused(DiagramMessage message) => new(DiagramText.Format(message));

    /// <summary>The refusal, its text from the message with its holes filled.</summary>
    private static InvalidDataException Refused(DiagramMessage message, object holes) =>
        new(DiagramText.Format(message, holes));
}
