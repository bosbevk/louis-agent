using LouisAgent.Api.Contracts.Admin.Evals;
using LouisAgent.Puls.Admin.Rates;

namespace LouisAgent.Puls.Admin.Evals;

/// <summary>
/// One run as the eval card shows it: the run itself, where its results came from (and which run that was), and each
/// mode's rates.
/// </summary>
public sealed record RunLine(EvalRunRow Run, RunOrigin Origin, string? Source, IReadOnlyList<ModeRate> Modes);
