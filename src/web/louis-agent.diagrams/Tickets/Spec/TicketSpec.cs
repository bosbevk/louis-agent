namespace LouisAgent.Diagrams.Tickets.Spec;

/// <summary>
/// A read-only card an agent shows, such as a booking request (F127.4.1): a band with a small kicker, its title, an
/// optional subtitle and status, then one to six rows, each a fact with its icon, and an optional note at the foot.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0118-a-diagram-is-data-our-renderer-draws.md">ADR 0118: a diagram is data our renderer draws</seealso>
public sealed record TicketSpec(
    string Kicker,
    string Title,
    IReadOnlyList<TicketRow> Rows,
    string? Subtitle = null,
    TicketStatus? Status = null,
    string? Note = null);
