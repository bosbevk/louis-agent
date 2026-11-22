namespace LouisAgent.Evals.Grading;

/// <summary>
/// The judge couldn't mark a criterion: it gave no answer, one that doesn't fit, or its model failed. It is never
/// turned into "not met", since that would blame the agent for the judge's trouble.
/// </summary>
/// <param name="message">Why the judge couldn't mark.</param>
/// <param name="inner">The model's own failure, when it was the model.</param>
public sealed class JudgeFailedException(string message, Exception? inner = null) : Exception(message, inner);
