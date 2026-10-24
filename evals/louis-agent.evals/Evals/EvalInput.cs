namespace LouisAgent.Evals.Evals;

/// <summary>What the user sends in a scenario: where they are, their time zone, and their messages in order.</summary>
public sealed record EvalInput(string? Place, string TimeZone, IReadOnlyList<string> Messages);
