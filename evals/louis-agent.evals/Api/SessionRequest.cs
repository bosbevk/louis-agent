namespace LouisAgent.Evals.Api;

/// <summary>A session an eval opens: its mode, where the user is, the task it runs, and the run's tag on its usage.</summary>
public sealed record SessionRequest(EvalMode Mode, string? Place, string TimeZone, string Task, string RunTag);
