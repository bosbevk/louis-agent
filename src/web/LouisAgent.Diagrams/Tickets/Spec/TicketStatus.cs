using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Tickets.Spec;

/// <summary>Where a ticket stands, in the pill on its band: "Not sent yet", or "Sent" in green once it went.</summary>
public sealed record TicketStatus(string Label, DiagramTone Tone = DiagramTone.Plain);
