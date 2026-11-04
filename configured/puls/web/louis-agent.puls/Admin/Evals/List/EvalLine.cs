namespace LouisAgent.Puls.Admin.Evals.List;

/// <summary>
/// One line of the Evals page's list: the eval, what it proves, its last run's status and start, and per mode the
/// last pass@k with its change; an eval that never ran has none of the last three.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record EvalLine(
    string Name,
    string Proves,
    string? Status,
    DateTimeOffset? LastAt,
    IReadOnlyList<ModeChange> Modes,
    bool? NeverRun,
    int Runs);
