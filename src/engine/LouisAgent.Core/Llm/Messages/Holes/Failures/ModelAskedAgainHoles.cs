namespace LouisAgent.Core.Llm.Messages.Holes.Failures;

/// <summary>
/// What a retry is logged with (F8): the failure's type, how long it waits, which try this is and how many there may
/// be.
/// </summary>
public sealed record ModelAskedAgainHoles(string Failure, int Wait, int Try, int Most);
