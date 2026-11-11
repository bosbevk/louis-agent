using LouisAgent.Diagrams.Icons;

namespace LouisAgent.Diagrams.Cards;

/// <summary>
/// One card of an overview: what a link names it by, its icon, its title, a project or type in a code chip beside
/// the title when it has one, and a line saying what it is.
/// </summary>
public sealed record Card(string Id, DiagramIcon Icon, string Title, string? Code, string Text);
