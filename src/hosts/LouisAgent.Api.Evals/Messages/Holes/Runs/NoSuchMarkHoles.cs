namespace LouisAgent.Api.Evals.Messages.Holes.Runs;

/// <summary>The run, and where the criterion it lacks would be: conversation, repeat, turn, criterion.</summary>
public sealed record NoSuchMarkHoles(string Id, string Conversation, int Repeat, int Turn, int Criterion);
