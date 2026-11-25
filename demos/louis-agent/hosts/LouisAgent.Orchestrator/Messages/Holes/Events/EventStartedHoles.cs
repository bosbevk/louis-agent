namespace LouisAgent.Orchestrator.Messages.Holes.Events;

/// <summary>
/// Which error of the batch is being triaged, and what failed: the call, its arguments and the exception.
/// </summary>
public sealed record EventStartedHoles(
    int Number,
    int Count,
    string ErrorId,
    string Method,
    string Arguments,
    string ExceptionType,
    string Message);
