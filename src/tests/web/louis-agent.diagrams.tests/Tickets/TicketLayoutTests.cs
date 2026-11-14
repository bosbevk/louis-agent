using LouisAgent.Diagrams.Engine.Svg;
using LouisAgent.Diagrams.Engine.Tickets;
using LouisAgent.Diagrams.Engine.Tickets.Placed;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Tickets.Spec;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Tests.Tickets;

/// <summary>
/// A ticket laid out and drawn (F127.4.1): a head with its kicker, title, subtitle and status pill, the rows with
/// an icon each, and a note in a well at the foot. Every word stays inside the card and clear of the pill, a long
/// value wraps and the card grows, and its colours are the page's when the page gives them.
/// </summary>
public class TicketLayoutTests
{
    /// <summary>The guesthouse's booking request, as the design canvas shows it.</summary>
    private static readonly TicketSpec Booking = new("BOOKING REQUEST", "The garden room",
        [
            new TicketRow(DiagramIcon.Calendar, "Dates", "Fri 14 to Sun 16 Nov · 2 nights"),
            new TicketRow(DiagramIcon.Users, "Guests", "2 adults"),
            new TicketRow(DiagramIcon.Paw, "Dog", "Welcome in this room"),
            new TicketRow(DiagramIcon.Mail, "Contact", "Anna · anna@example.com")
        ], "Seemeeu Guesthouse · Yzerfontein", new TicketStatus("Not sent yet", DiagramTone.Amber),
        "The guesthouse confirms by email. Nothing is paid here.");

    /// <summary>
    /// A ticket is laid out 320 wide (Louis's pick on the phone check): a phone's chat gives it about 275, so it
    /// shrinks to no less than about 85% and its words stay readable.
    /// </summary>
    [Test]
    public void ATicket_IsLaidOutForAPhonesChat() =>
        Assert.That(TicketLayout.Place(Booking).Card.Width, Is.EqualTo(320));

    /// <summary>Every line of words stands inside the card, its padding kept on both sides.</summary>
    [Test]
    public void EveryWord_StaysInsideTheCard()
    {
        var placed = TicketLayout.Place(Booking);

        Assert.That(placed.Words.Where(words => words.At.X < TicketLayout.Pad - 0.01 ||
                                                words.End.X > placed.Card.Width - TicketLayout.Pad + 0.01)
            .Select(words => words.Text), Is.Empty);
    }

    /// <summary>
    /// A line of words runs as wide as its own kind's face says, and ends that far after where it starts, on its
    /// baseline: a title's letters are wider than a note's.
    /// </summary>
    [Test]
    public void ALineOfWords_EndsWhereItsKindsFaceSays()
    {
        var title = TicketLayout.Place(Booking).Words.Single(words => words.Kind == TicketWords.Title);

        Assert.Multiple(() =>
        {
            Assert.That(title.Width, Is.EqualTo(TicketWords.Title.Face.Width("The garden room")));
            Assert.That((title.End.X, title.End.Y), Is.EqualTo((title.At.X + title.Width, title.At.Y)));
            Assert.That(TicketWords.Title.Face.Width("room"), Is.GreaterThan(TicketWords.Note.Face.Width("room")));
        });
    }

    /// <summary>The head's words that share the pill's height end before it, so the status never covers them.</summary>
    [Test]
    public void TheHeadsWords_EndBeforeThePill()
    {
        var placed = TicketLayout.Place(Booking with { Title = "The garden room, with its own door to the lawn" });
        var pill = placed.Pill!.Box;

        Assert.That(placed.Words.Where(words => words.At.Y - 14 < pill.Bottom && words.At.Y > pill.Top)
            .Where(words => words.End.X > pill.Left - 6).Select(words => words.Text), Is.Empty);
    }

    /// <summary>A long value wraps onto a second line, and the card grows by that line.</summary>
    [Test]
    public void ALongValue_Wraps_AndTheCardGrows()
    {
        var shortOne = TicketLayout.Place(Booking);
        var longOne = TicketLayout.Place(Booking with
        {
            Rows =
            [
                .. Booking.Rows.Take(3), new TicketRow(DiagramIcon.Mail, "Contact",
                    "Anna van der Merwe · anna.vandermerwe@example.com · 082 555 0123")
            ]
        });

        Assert.Multiple(() =>
        {
            Assert.That(longOne.Words.Count(words => words.Kind == TicketWords.Value),
                Is.GreaterThan(shortOne.Words.Count(words => words.Kind == TicketWords.Value)));
            Assert.That(longOne.Card.Height, Is.GreaterThan(shortOne.Card.Height));
        });
    }

    /// <summary>Each row stands under the one before, never on it.</summary>
    [Test]
    public void TheRows_StandOneUnderAnother()
    {
        var rows = TicketLayout.Place(Booking).Rows;

        Assert.That(rows.Zip(rows.Skip(1)).All(pair => pair.Second.Box.Top >= pair.First.Box.Bottom), Is.True);
    }

    /// <summary>
    /// The note stands in a well at the card's foot: its words inside the well with room either side, the well
    /// inside the card under the last row, and the card ending a little under it. A ticket with no note has no
    /// well and ends under its last row.
    /// </summary>
    [Test]
    public void TheNote_StandsInAWell_AtTheCardsFoot()
    {
        var placed = TicketLayout.Place(Booking);
        var well = placed.Note!;
        var note = placed.Words.Where(words => words.Kind == TicketWords.Note).ToList();
        var bare = TicketLayout.Place(Booking with { Note = null });

        Assert.Multiple(() =>
        {
            Assert.That(note, Has.Count.EqualTo(2), "the note wraps inside its well");
            Assert.That(note.Select(words => words.At.X), Is.All.GreaterThan(well.Left));
            Assert.That(note.Select(words => words.End.X), Is.All.LessThan(well.Right));
            Assert.That(note.Select(words => words.At.Y), Is.All.InRange(well.Top, well.Bottom));
            Assert.That(placed.Card.Holds(well), Is.True);
            Assert.That(well.Top, Is.GreaterThan(placed.Rows[^1].Box.Bottom));
            Assert.That(placed.Card.Bottom - well.Bottom, Is.EqualTo(TicketLayout.Pad));
            Assert.That(bare.Note, Is.Null);
            Assert.That(bare.Card.Bottom, Is.EqualTo(bare.Rows[^1].Box.Bottom + TicketLayout.Pad));
        });
    }

    /// <summary>
    /// Drawn, its colours are the page's variables with the look's as the fallback, so a business's window dresses
    /// it in its brand (Q3) and the site needs nothing. The paper under the glass is solid, so the words read on
    /// any page.
    /// </summary>
    [Test]
    public void TheColours_AreThePagesWithTheSitesAsFallback()
    {
        string svg = DiagramRenderers.Render(new DiagramSource(DiagramKind.Ticket, "Booking request", Ticket: Booking));

        Assert.Multiple(() =>
        {
            Assert.That(svg, Does.Contain("var(--ticket-paper, var(--ground))"));
            Assert.That(svg, Does.Contain("var(--ticket-ink, var(--text))"));
            Assert.That(svg, Does.Contain("var(--ticket-accent, var(--violet-line))"));
        });
    }

    /// <summary>
    /// The card wears the look every picture of ours wears, and nothing of a paper ticket (FX156, Louis: "it must
    /// follow the theme", "glass but not a ticket"): a glass card with the accent along its top and a light
    /// behind its head, and no band, no torn edge and no notch bitten out of its sides.
    /// </summary>
    [Test]
    public void TheCard_IsGlassInOurLook_AndNothingOfATicket()
    {
        string svg = DiagramRenderers.Render(new DiagramSource(DiagramKind.Ticket, "Booking request", Ticket: Booking));

        Assert.Multiple(() =>
        {
            Assert.That(svg, Does.Contain("class=\"mrq-glass ticket\""), "the card is the look's glass");
            Assert.That(svg, Does.Contain("class=\"mrq-edge\""), "with the accent along its top");
            Assert.That(svg, Does.Contain("class=\"ticket-light\""), "and a light behind its head");
            Assert.That(svg, Does.Not.Contain("ticket-band"), "no band");
            Assert.That(svg, Does.Not.Contain("ticket-tear"), "no torn edge");
            Assert.That(svg, Does.Not.Contain("A8 8"), "no notch bitten out of a side");
        });
    }

    /// <summary>The booking request drawn matches its golden file.</summary>
    [Test]
    public void ATicket_MatchesTheGoldenFile() =>
        Golden.Match("ticket.svg",
            DiagramRenderers.Render(new DiagramSource(DiagramKind.Ticket, "Booking request", Ticket: Booking)));
}
