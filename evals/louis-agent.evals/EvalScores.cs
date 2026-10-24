namespace LouisAgent.Evals;

/// <summary>
/// An eval's scores for one conversation: written as snake_case JSON into the protocol's <c>conversation</c> line. Each eval
/// has its own record (the cook's, the routing's).
/// </summary>
public abstract record EvalScores;
