namespace LouisAgent.Evals.Runs;

/// <summary>Which play this is: of which run, which conversation, and which repeat of it.</summary>
/// <param name="Run">The run's id.</param>
/// <param name="Conversation">The conversation's id in the eval file.</param>
/// <param name="Repeat">Which repeat, from 1.</param>
public sealed record RunTrial(string Run, string Conversation, int Repeat);
