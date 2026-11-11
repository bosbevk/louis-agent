using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Diagrams.Cards;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Messages;
using LouisAgent.Diagrams.Sequence.Spec;

namespace LouisAgent.Diagrams.Sources;

/// <summary>Reads a diagram's JSON file into its records, and refuses one it can't draw, saying why.</summary>
/// <remarks>
/// The file is a semantic model (Fowler, <i>Domain-Specific Languages</i>): plain data a person writes, read once into
/// records the renderer interprets. Everything that would make a broken picture is refused here, by name (an unknown
/// kind, a missing part, an arrow or a link to an id nothing has, a misspelt field), so the renderer never has to
/// guess and a typo never becomes a box without its arrow.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public static class DiagramReader
{
    /// <summary>The diagram the JSON describes.</summary>
    public static DiagramSource Read(string json)
    {
        var kind = KindOf(json);
        var source = Deserialize(json);
        if (string.IsNullOrWhiteSpace(source.Title)) throw Refused(DiagramMessage.MissingTitle, new { });
        CheckPart(kind, source);
        if (source.Flow is { } flow) CheckFlow(flow);
        if (source.Cards is { } cards) CheckCards(cards);
        if (source.Sequence is { } sequence) CheckSequence(sequence);
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
            throw Refused(DiagramMessage.Unreadable, new { Reason = ex.Message });
        }

        if (SettingChoice.TryParse<DiagramKind>(text, out var parsed)) return parsed!.Value;
        throw Refused(DiagramMessage.UnknownKind,
            new
            {
                Value = text,
                Kinds = string.Join(", ", SettingChoice.Names<DiagramKind>())
            });
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
            throw Refused(DiagramMessage.Unreadable, new { Reason = ex.Message });
        }
        // A sequence's step without its "step" field can't be told apart: System.Text.Json says so this way.
        catch (NotSupportedException ex)
        {
            throw Refused(DiagramMessage.Unreadable, new { Reason = ex.Message });
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
            _ => false
        };
        if (!hasPart) throw Refused(DiagramMessage.MissingPart, new { Kind = kind.Wire() });
    }

    /// <summary>Every box's group is one the flow declares, and every arrow joins two boxes the flow has.</summary>
    private static void CheckFlow(GraphSpec flow)
    {
        // A box's group must be one the diagram declares, so its frame has its label.
        var groups = (flow.Groups ?? []).Select(group => group.Id).ToHashSet(StringComparer.Ordinal);
        var astray = flow.Nodes.FirstOrDefault(node => node.Group is { } group && !groups.Contains(group));
        if (astray is not null)
            throw Refused(DiagramMessage.UnknownGroup,
                new
                {
                    astray.Id,
                    astray.Group
                });

        var ids = flow.Nodes.Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
        var stray = flow.Edges.FirstOrDefault(edge => !ids.Contains(edge.From) || !ids.Contains(edge.To));
        if (stray is null) return;
        throw Refused(DiagramMessage.UnknownBox,
            new
            {
                stray.From,
                stray.To,
                Id = ids.Contains(stray.From) ? stray.To : stray.From
            });
    }

    /// <summary>Every link joins two cards the overview has.</summary>
    private static void CheckCards(CardsSpec cards)
    {
        var ids = cards.Rows.SelectMany(row => row.Cards).Select(card => card.Id).ToHashSet(StringComparer.Ordinal);
        var stray = cards.Links.FirstOrDefault(link => !ids.Contains(link.From) || !ids.Contains(link.To));
        if (stray is null) return;
        throw Refused(DiagramMessage.UnknownCard,
            new
            {
                stray.From,
                stray.To,
                Id = ids.Contains(stray.From) ? stray.To : stray.From
            });
    }

    /// <summary>
    /// A sequence has a step at least, a note stands over someone, and every step names participants the chart has.
    /// </summary>
    private static void CheckSequence(SequenceSpec sequence)
    {
        if (sequence.Steps.Count == 0) throw Refused(DiagramMessage.NoSteps, new { });
        if (sequence.Steps.OfType<NoteStep>().FirstOrDefault(note => note.Over.Count == 0) is { } bare)
            throw Refused(DiagramMessage.NoteOverNoOne, new { bare.Text });

        var ids = sequence.Participants.Select(head => head.Id).ToHashSet(StringComparer.Ordinal);
        var stray = sequence.Steps
            .SelectMany(step => step.Names().Where(id => !ids.Contains(id)).Select(id => (Step: step, Id: id)))
            .FirstOrDefault();
        if (stray == default) return;
        throw Refused(DiagramMessage.UnknownParticipant,
            new
            {
                stray.Id,
                Step = stray.Step.ToString()
            });
    }

    /// <summary>The refusal, its text from the message enum.</summary>
    private static InvalidDataException Refused(DiagramMessage message, object values) =>
        new(DiagramText.Format(message, values));
}
