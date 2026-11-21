namespace LouisAgent.Diagrams.Tickets.Inputs;

/// <summary>A ticket's status as an agent writes it: its words, and a tone by name ("green" once sent).</summary>
public sealed record TicketStatusInput(string Label, string? Tone = null);
