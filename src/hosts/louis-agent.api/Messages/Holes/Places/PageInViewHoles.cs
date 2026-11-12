namespace LouisAgent.Api.Messages.Holes.Places;

/// <summary>The section on the visitor's screen, the page it is on, and the card open there, if one is.</summary>
public sealed record PageInViewHoles(string Id, string Heading, string Url, string Open);
