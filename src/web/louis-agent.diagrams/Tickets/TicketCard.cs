using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Messages;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Tickets.Inputs;
using LouisAgent.Diagrams.Tickets.Spec;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Tickets;

/// <summary>
/// A ticket from a tool call's parts (F127.4.1): the site pack's <c>ShowCard</c> checks what the agent wrote with
/// it, and the widget reads the same call back into the card it draws.
/// </summary>
/// <remarks>
/// One translation from a call to a ticket, in the library both sides already use, so the pack that says "shown"
/// and the page that draws it can never read a call two ways (Q2). The checks are the reader's own: the ticket is
/// written and read back, so every rule a file has holds for a card too.
/// </remarks>
public static class TicketCard
{
    /// <summary>A call's names in any case, as a model may send them.</summary>
    private static readonly JsonSerializerOptions CallJson = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// The ticket the parts describe, its title the kicker and the title together; refused with the reason when an
    /// icon or a tone isn't one there is, or the reader wouldn't draw it.
    /// </summary>
    public static DiagramSource Of(string kicker, string title, IReadOnlyList<TicketRowInput> rows,
        string? subtitle = null, TicketStatusInput? status = null, string? note = null)
    {
        var spec = new TicketSpec(kicker, title, rows.Select(RowOf).ToList(), subtitle, StatusOf(status), note);
        var source = new DiagramSource(DiagramKind.Ticket, $"{kicker}: {title}", Ticket: spec);
        return DiagramReader.Read(DiagramWriter.Write(source));
    }

    /// <summary>The ticket a call's input describes, or none when the input can't be one.</summary>
    public static DiagramSource? FromCall(string? input)
    {
        if (input is null) return null;
        // A model's input that isn't a whole card draws nothing, rather than failing the visitor's page.
        try
        {
            var call = JsonSerializer.Deserialize<TicketCall>(input, CallJson);
            return call is null ? null : Of(call.Kicker, call.Title, call.Rows, call.Subtitle, call.Status, call.Note);
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or ArgumentNullException)
        {
            return null;
        }
    }

    /// <summary>A row with its icon read by name; refused, naming the icons, when there is none of that name.</summary>
    private static TicketRow RowOf(TicketRowInput row, int at)
    {
        if (SettingChoice.TryParse<DiagramIcon>(row.Icon, out var icon))
            return new TicketRow(icon!.Value, row.Label, row.Value);
        throw Refused(DiagramMessage.UnknownIcon,
            new
            {
                Number = at + 1,
                row.Icon,
                Icons = string.Join(", ", SettingChoice.Names<DiagramIcon>())
            });
    }

    /// <summary>The status with its tone read by name, plain when none is given.</summary>
    private static TicketStatus? StatusOf(TicketStatusInput? status)
    {
        if (status is null) return null;
        if (status.Tone is null) return new TicketStatus(status.Label);
        if (SettingChoice.TryParse<DiagramTone>(status.Tone, out var tone))
            return new TicketStatus(status.Label, tone!.Value);
        throw Refused(DiagramMessage.UnknownTone,
            new
            {
                status.Tone,
                Tones = string.Join(", ", SettingChoice.Names<DiagramTone>())
            });
    }

    /// <summary>The refusal, its text from the message enum.</summary>
    private static InvalidDataException Refused(DiagramMessage message, object values) =>
        new(DiagramText.Format(message, values));

    /// <summary>A <c>ShowCard</c> call's input, as the stream carries it.</summary>
    private sealed record TicketCall(
        string Kicker,
        string Title,
        IReadOnlyList<TicketRowInput> Rows,
        string? Subtitle = null,
        TicketStatusInput? Status = null,
        string? Note = null);
}
