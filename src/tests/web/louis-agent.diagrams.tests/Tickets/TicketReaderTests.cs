using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Tickets.Spec;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Tests.Tickets;

/// <summary>
/// A ticket is data like every other kind (F127.4.1): a band with its kicker, title and status, rows with an icon
/// each, and a note. The reader refuses a ticket it couldn't draw well, saying why, so an agent that wrote it can fix
/// it.
/// </summary>
public class TicketReaderTests
{
    /// <summary>A guesthouse's booking request: two rows, a status and a note.</summary>
    private const string Ticket = """
                                  {
                                    "kind": "ticket",
                                    "title": "Booking request: the garden room",
                                    "ticket": {
                                      "kicker": "BOOKING REQUEST",
                                      "title": "The garden room",
                                      "subtitle": "Seemeeu Guesthouse · Yzerfontein",
                                      "status": { "label": "Not sent yet", "tone": "amber" },
                                      "rows": [
                                        { "icon": "users", "label": "Guests", "value": "2 adults" },
                                        { "icon": "mail", "label": "Contact", "value": "Anna · anna@example.com" }
                                      ],
                                      "note": "The guesthouse confirms by email."
                                    }
                                  }
                                  """;

    /// <summary>A ticket reads into its band, its rows and its note.</summary>
    [Test]
    public void ATicket_ReadsIntoItsParts()
    {
        var ticket = DiagramReader.Read(Ticket).Ticket!;

        Assert.Multiple(() =>
        {
            Assert.That((ticket.Kicker, ticket.Title, ticket.Subtitle),
                Is.EqualTo(("BOOKING REQUEST", "The garden room", (string?)"Seemeeu Guesthouse · Yzerfontein")));
            Assert.That(ticket.Status, Is.EqualTo(new TicketStatus("Not sent yet", DiagramTone.Amber)));
            Assert.That(ticket.Rows,
                Is.EqualTo(new[]
                {
                    new TicketRow(DiagramIcon.Users, "Guests", "2 adults"),
                    new TicketRow(DiagramIcon.Mail, "Contact", "Anna · anna@example.com")
                }));
            Assert.That(ticket.Note, Is.EqualTo("The guesthouse confirms by email."));
        });
    }

    /// <summary>A ticket with no rows says nothing worth a card, and is refused.</summary>
    [Test]
    public void ATicket_WithNoRows_IsRefused()
    {
        string json = DiagramWriter.Write(new DiagramSource(DiagramKind.Ticket, "Empty",
            Ticket: new TicketSpec("KICKER", "Title", [])));

        Assert.That(() => DiagramReader.Read(json),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("one to six rows"));
    }

    /// <summary>More than six rows don't fit a chat's card, and are refused.</summary>
    [Test]
    public void ATicket_WithMoreThanSixRows_IsRefused()
    {
        var rows = Enumerable.Range(1, 7).Select(n => new TicketRow(DiagramIcon.Info, $"Row {n}", "a value")).ToList();
        string json = DiagramWriter.Write(new DiagramSource(DiagramKind.Ticket, "Too long",
            Ticket: new TicketSpec("KICKER", "Title", rows)));

        Assert.That(() => DiagramReader.Read(json),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("one to six rows"));
    }

    /// <summary>A ticket's own title is its headline: without it the band says nothing.</summary>
    [Test]
    public void ATicket_WithAnEmptyTitle_IsRefused()
    {
        string json = Ticket.Replace("\"title\": \"The garden room\"", "\"title\": \" \"", StringComparison.Ordinal);

        Assert.That(() => DiagramReader.Read(json),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("ticket needs its"));
    }

    /// <summary>A row without its label or its value is half a fact, and is refused, naming the row.</summary>
    [Test]
    public void ARow_WithoutItsValue_IsRefused()
    {
        string json = Ticket.Replace("\"value\": \"2 adults\"", "\"value\": \"\"", StringComparison.Ordinal);

        Assert.That(() => DiagramReader.Read(json),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("Row 1"));
    }

    /// <summary>An icon the library doesn't draw is refused where it stands.</summary>
    [Test]
    public void ARow_WithAnUnknownIcon_IsRefused()
    {
        string json = Ticket.Replace("\"icon\": \"users\"", "\"icon\": \"rocket\"", StringComparison.Ordinal);

        Assert.That(() => DiagramReader.Read(json),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("icon"));
    }

    /// <summary>Written, a ticket reads back the same.</summary>
    [Test]
    public void ATicket_IsWrittenAndReadBackTheSame()
    {
        string json = DiagramWriter.Write(DiagramReader.Read(Ticket));

        Assert.That(DiagramWriter.Write(DiagramReader.Read(json)), Is.EqualTo(json));
    }
}
