using LouisAgent.Diagrams.Icons;

namespace LouisAgent.Diagrams.Tickets.Spec;

/// <summary>One fact on a ticket: its icon, what it is ("Dates") and what it says ("Fri to Sun · 2 nights").</summary>
public sealed record TicketRow(DiagramIcon Icon, string Label, string Value);
