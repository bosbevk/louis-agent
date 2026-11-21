namespace LouisAgent.Evals.Api;

/// <summary>
/// A session an eval opens: its mode, where the user is, the task it runs, the run's tag on its usage, and what it does with the web
/// (<paramref name="Replay"/>: every eval session sends one, so the API knows it is an eval; F18 step 4a).
/// </summary>
public sealed record SessionRequest(
    EvalMode Mode,
    string? Place,
    string TimeZone,
    string Task,
    int Repeat,
    string RunTag,
    WebReplayRequest Replay);
