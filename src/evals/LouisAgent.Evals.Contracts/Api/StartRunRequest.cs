using LouisAgent.Evals.Contracts.Runs;

namespace LouisAgent.Evals.Contracts.Api;

/// <summary>What a run is started with: live or a replay, and one repeat or three.</summary>
/// <param name="Kind">Live, which spends money, or a replay of the eval's last live run, which is free.</param>
/// <param name="Repeats">How many times each conversation is played: 1 or 3.</param>
public sealed record StartRunRequest(RunKind Kind, int Repeats);
