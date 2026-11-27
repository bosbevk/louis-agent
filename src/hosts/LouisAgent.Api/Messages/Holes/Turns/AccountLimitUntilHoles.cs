namespace LouisAgent.Api.Messages.Holes.Turns;

/// <summary>
/// What a turn stopped by an account limit tells the chat (F8): when the agent answers again, in UTC.
/// </summary>
public sealed record AccountLimitUntilHoles(string Resumes);
