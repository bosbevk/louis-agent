namespace LouisAgent.Api.Messages.Holes.Turns;

/// <summary>
/// What a message refused as still answering is logged with (FX252): the session, how long its turn has run, and
/// whether that turn was already asked to stop.
/// </summary>
public sealed record TurnRefusedBusyHoles(string Session, int Seconds, string Cancelled);
