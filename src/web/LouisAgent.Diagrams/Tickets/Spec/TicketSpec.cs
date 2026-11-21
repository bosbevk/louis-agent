namespace LouisAgent.Diagrams.Tickets.Spec;

/// <summary>
/// A read-only card an agent shows, such as a booking request (F127.4.1): a band with a small topic, its title, an
/// optional subtitle and status, then one to six rows, each a fact with its icon, and an optional note at the foot.
/// </summary>
/// <remarks>
/// "Ticket" names the record, and nothing of how it is drawn: since FX156 it is a glass card in the pictures'
/// look. The kind keeps the name for now (picked unattended, 2026-11-15, Louis's to overrule). The other two
/// ways were <c>chat-card</c>, a kind of two words whose JSON field could not be its name as every other kind's
/// is, and <c>card</c> with the <c>cards</c> kind renamed <c>overview</c>, which rewrites every overview.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0118-a-diagram-is-data-our-renderer-draws.md">ADR 0118: a diagram is data our renderer draws</seealso>
public sealed record TicketSpec(
    string Topic,
    string Title,
    IReadOnlyList<TicketRow> Rows,
    string? Subtitle = null,
    TicketStatus? Status = null,
    string? Note = null);
