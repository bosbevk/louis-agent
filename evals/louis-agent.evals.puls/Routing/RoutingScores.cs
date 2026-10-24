using LouisAgent.Evals.Evals;

namespace LouisAgent.Evals.Puls.Routing;

/// <summary>One routing question as numbers (F31, the routing check's scores document): the call made, not the words.</summary>
public sealed record RoutingScores(
    string? Role,
    string? ExpectedRole,
    bool RoutedRight,
    bool AnsweredItself,
    int Delegations,
    int PulsCalls,
    int SpecialistCalls,
    IReadOnlyList<string> Hosts,
    int Errors) : EvalScores;
