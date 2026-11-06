namespace LouisAgent.Evals.Datasets.Scenarios;

/// <summary>
/// What the user sends in a scenario: where they are, their time zone, and their messages in order. A scenario in
/// another language can name the role it opens in, as a chat in that language would; with none, it's the mode's.
/// </summary>
public sealed record EvalInput(string? Place, string TimeZone, IReadOnlyList<string> Messages, string? Role = null);
