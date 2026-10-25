namespace LouisAgent.Evals.Runner.Evals.Stub.Scoring;

/// <summary>The stub's scores, as the Python stub writes them: its run is fixed, so the route is always right, and the turns counted.</summary>
public sealed record StubScores(bool RoutedRight, int Turns) : EvalScores;
