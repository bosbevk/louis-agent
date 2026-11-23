namespace LouisAgent.Web.Evals.Client;

/// <summary>
/// Where a criterion's mark is in a run: its conversation and repeat, then the turn and the criterion counted from 1,
/// as the pages show them. A flip is sent to this place's own address.
/// </summary>
/// <param name="Run">The run's id.</param>
/// <param name="Conversation">The conversation's id.</param>
/// <param name="Repeat">The repeat's number.</param>
/// <param name="Turn">The turn, from 1.</param>
/// <param name="Criterion">The criterion in its turn, from 1.</param>
public sealed record MarkPlace(string Run, string Conversation, int Repeat, int Turn, int Criterion);
