namespace LouisAgent.Evals.Runner.Catalog;

/// <summary>One line of <c>run --list</c>: an eval as the API's catalogue reads it (F18 step 4.3).</summary>
public sealed record ListedEval(string Name, string? Identity, int Version, string Proves);
