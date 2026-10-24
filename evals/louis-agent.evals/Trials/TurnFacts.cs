namespace LouisAgent.Evals.Trials;

/// <summary>
/// What a turn's record in the protocol says about it (F18 step 4.1): who answered, the sub-session it continued, the calls Puls
/// made and the specialists' calls, the hosts they read, and the error. The eval says these, from its own rules (Puls's come
/// from TurnView); the runner writes them.
/// </summary>
public sealed record TurnFacts(string? Role, string? Continued, int PulsCalls, int SpecialistCalls, IReadOnlyList<string> Hosts, string? Error);
