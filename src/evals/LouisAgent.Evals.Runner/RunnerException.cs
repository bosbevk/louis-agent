namespace LouisAgent.Evals.Runner;

/// <summary>A refusal of the runner: its text is a <see cref="Messages.RunnerMessage"/> member. The host writes it to stderr and exits non-zero.</summary>
public sealed class RunnerException(string message) : Exception(message);
