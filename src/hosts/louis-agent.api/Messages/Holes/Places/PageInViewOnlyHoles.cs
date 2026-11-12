namespace LouisAgent.Api.Messages.Holes.Places;

/// <summary>A later message's page: its address, the section on the visitor's screen, and the card open there.</summary>
public sealed record PageInViewOnlyHoles(string Url, string Id, string Heading, string Open);
