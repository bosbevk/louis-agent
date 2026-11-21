using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Evals.Protocol.Parts.Run;

/// <summary>
/// Which model one part of the run used, in which mode: the agent, a specialist's role, a minion, a judge.
/// </summary>
public sealed record ModelInfo(string Mode, ModelLevel Level, string? Role, string? Model);
