namespace LouisAgent.Api.Evals.Conversations;

/// <summary>A flip's body: why the person flipped the mark, in one line, or none.</summary>
/// <param name="Note">Why, or none.</param>
public sealed record FlipRequest(string? Note);
