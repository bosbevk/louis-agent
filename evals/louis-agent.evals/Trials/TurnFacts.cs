namespace LouisAgent.Evals.Trials;

/// <summary>
/// What a turn's record in the protocol says about it (F18 step 4.1): who answered, the sub-session it continued, the calls the
/// mode's agent made (the protocol's puls_calls, a key the API reads) and the specialists' calls, the hosts they read, and the
/// error. The eval says these, from its own rules (an identity's come from its own view of the turn); the runner writes them.
/// </summary>
public sealed record TurnFacts(
    string? Role,
    string? Continued,
    int AgentCalls,
    int SpecialistCalls,
    IReadOnlyList<string> Hosts,
    string? Error);
