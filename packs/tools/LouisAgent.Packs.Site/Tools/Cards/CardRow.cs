namespace LouisAgent.Packs.Site.Tools.Cards;

/// <summary>
/// One fact on a card as the model writes it: the icon by its name ("calendar"), what the fact is and what it says.
/// The same names as the diagrams library's ticket, so the widget reads the call as a ticket.
/// </summary>
public sealed record CardRow(string Icon, string Label, string Value);
