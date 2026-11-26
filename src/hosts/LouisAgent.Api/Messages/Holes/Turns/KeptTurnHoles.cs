namespace LouisAgent.Api.Messages.Holes.Turns;

/// <summary>A chat's turn by its number, as the store keeps it.</summary>
public sealed record KeptTurnHoles(string Session, int Number);
