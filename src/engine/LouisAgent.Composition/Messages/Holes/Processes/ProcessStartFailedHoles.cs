namespace LouisAgent.Composition.Messages.Holes.Processes;

/// <summary>The program that couldn't start, and why.</summary>
public sealed record ProcessStartFailedHoles(string Program, string Reason);
