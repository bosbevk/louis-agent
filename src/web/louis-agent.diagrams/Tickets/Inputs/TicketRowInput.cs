namespace LouisAgent.Diagrams.Tickets.Inputs;

/// <summary>
/// A row as an agent writes it in a tool call: the icon by its name ("calendar"), what the fact is and what it says.
/// </summary>
public sealed record TicketRowInput(string Icon, string Label, string Value);
