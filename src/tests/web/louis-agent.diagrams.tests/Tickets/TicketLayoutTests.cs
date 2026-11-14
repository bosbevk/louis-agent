using LouisAgent.Diagrams.Engine.Svg;
using LouisAgent.Diagrams.Engine.Tickets;
using LouisAgent.Diagrams.Engine.Tickets.Placed;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Tickets.Spec;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Tests.Tickets;

/// <summary>
/// A ticket laid out and drawn (F127.4.1): a band with its kicker, title, subtitle and status pill, a torn edge, the
/// rows with an icon each, and a note at the foot. Every word stays inside the card and clear of the pill, a long
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
        Assert.That(TicketLayout.Place(Booking).Width, Is.EqualTo(320));

    /// <summary>Every line of words stands inside the card, its padding kept on both sides.</summary>
    [Test]
    public void EveryWord_StaysInsideTheCard()
    {
        var placed = TicketLayout.Place(Booking);

        Assert.That(placed.Words.Where(words => words.At.X < TicketLayout.Pad - 0.01 ||
                                                words.End.X > placed.Width - TicketLayout.Pad + 0.01)
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

    /// <summary>The band's words that share the pill's height end before it, so the status never covers them.</summary>
    [Test]
    public void TheBandsWords_EndBeforeThePill()
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
            Assert.That(longOne.Height, Is.GreaterThan(shortOne.Height));
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
    /// Drawn, its colours are the page's variables with the site's as the fallback, so a business's window dresses
    /// it in its brand (Q3) and the site needs nothing.
    /// </summary>
    [Test]
    public void TheColours_AreThePagesWithTheSitesAsFallback()
    {
        string svg = DiagramRenderers.Render(new DiagramSource(DiagramKind.Ticket, "Booking request", Ticket: Booking));

        Assert.Multiple(() =>
        {
            Assert.That(svg, Does.Contain("var(--ticket-band, var(--t-band))"));
            Assert.That(svg, Does.Contain("var(--ticket-ink, var(--text))"));
        });
    }

    /// <summary>The booking request drawn matches its golden file.</summary>
    [Test]
    public void ATicket_MatchesTheGoldenFile() =>
        Golden.Match("ticket.svg",
            DiagramRenderers.Render(new DiagramSource(DiagramKind.Ticket, "Booking request", Ticket: Booking)));
}
