namespace LouisAgent.Evals.Contracts.Api;

/// <summary>Every eval this host has, by name.</summary>
/// <param name="Evals">The evals, in order of their names.</param>
public sealed record EvalsReply(IReadOnlyList<EvalRow> Evals);
