namespace LouisAgent.Evals.Messages.Holes.Files;

/// <summary>The words that are no fact, and the first words a fact may start with.</summary>
public sealed record NotAFactHoles(string Words, string Kinds);
