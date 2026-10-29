namespace LouisAgent.Puls.Admin.Starting;

/// <summary>What a card's Run button asks the page for: run this check, this many times.</summary>
/// <param name="Check">The check to run.</param>
/// <param name="Repeats">How many repeats the card's switch is on.</param>
public sealed record StartRequest(string Check, int Repeats);
