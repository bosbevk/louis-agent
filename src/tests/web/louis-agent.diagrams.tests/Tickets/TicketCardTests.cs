using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Tickets;
using LouisAgent.Diagrams.Tickets.Inputs;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Tests.Tickets;

/// <summary>
/// A ticket from a tool call (F127.4.1): the site pack's <c>ShowCard</c> checks the agent's parts with it, and the
/// widget reads the same call back into the card it draws, so the two can never disagree about what a card says.
/// </summary>
public class TicketCardTests
{
    /// <summary>The rows an agent writes, icons by name.</summary>
    private static readonly TicketRowInput[] Rows =
    [
        new("calendar", "Dates", "Fri 14 to Sun 16 Nov"),
        new("paw", "Dog", "Welcome in this room")
    ];

    /// <summary>An agent's parts become a ticket diagram, its title the kicker and the title together.</summary>
    [Test]
    public void TheParts_BecomeATicket()
    {
        var source = TicketCard.Of("BOOKING REQUEST", "The garden room", Rows, "Seemeeu Guesthouse",
            new TicketStatusInput("Sent", "green"), "The guesthouse confirms by email.");

        Assert.Multiple(() =>
        {
            Assert.That(source.Kind, Is.EqualTo(DiagramKind.Ticket));
            Assert.That(source.Title, Is.EqualTo("BOOKING REQUEST: The garden room"));
            Assert.That(source.Ticket!.Rows.Select(row => row.Icon),
                Is.EqualTo(new[] { DiagramIcon.Calendar, DiagramIcon.Paw }));
            Assert.That(source.Ticket.Status!.Tone, Is.EqualTo(DiagramTone.Green));
        });
    }

    /// <summary>An icon the library doesn't draw is refused, naming it and the ones it does.</summary>
    [Test]
    public void AnUnknownIcon_IsRefused_NamingTheIcons()
    {
        Assert.That(() => TicketCard.Of("K", "T", [new TicketRowInput("rocket", "Ship", "Soon")]),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("'rocket'").And.Message.Contains("calendar"));
    }

    /// <summary>A tone the library doesn't have is refused, naming the tones.</summary>
    [Test]
    public void AnUnknownTone_IsRefused_NamingTheTones()
    {
        Assert.That(() => TicketCard.Of("K", "T", Rows, status: new TicketStatusInput("Sent", "gold")),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("'gold'").And.Message.Contains("green"));
    }

    /// <summary>What the reader refuses is refused here too, with its reason: a ticket without rows.</summary>
    [Test]
    public void ACardWithoutRows_IsRefused_WithTheReadersReason()
    {
        Assert.That(() => TicketCard.Of("K", "T", []),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("one to six rows"));
    }

    /// <summary>
    /// The call's input, as the stream carries it, reads back into the same ticket, its names in any case.
    /// </summary>
    [Test]
    public void ACallsInput_ReadsBackIntoTheSameTicket()
    {
        const string input = """
                             {"kicker":"BOOKING REQUEST","title":"The garden room",
                              "rows":[{"Icon":"calendar","label":"Dates","value":"Fri 14 to Sun 16 Nov"},
                                      {"icon":"paw","label":"Dog","value":"Welcome in this room"}],
                              "status":{"label":"Sent","tone":"green"}}
                             """;

        var source = TicketCard.FromCall(input);

        Assert.That(source, Is.EqualTo(TicketCard.Of("BOOKING REQUEST", "The garden room", Rows,
            status: new TicketStatusInput("Sent", "green"))).Using<DiagramSource>(SameTicket));
    }

    /// <summary>A call whose input can't be a ticket draws nothing, rather than failing the chat.</summary>
    [TestCase("not json")]
    [TestCase("""{"kicker":"K","title":"T","rows":[]}""")]
    [TestCase("""{"kicker":"K","title":"T","rows":[{"icon":"rocket","label":"a","value":"b"}]}""")]
    [TestCase(null)]
    public void ABadCall_DrawsNothing(string? input) => Assert.That(TicketCard.FromCall(input), Is.Null);

    /// <summary>Two tickets say the same when their writing is the same.</summary>
    private static bool SameTicket(DiagramSource a, DiagramSource b) =>
        DiagramWriter.Write(a) == DiagramWriter.Write(b);
}
