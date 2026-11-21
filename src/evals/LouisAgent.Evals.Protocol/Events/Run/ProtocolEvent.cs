using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Run;

namespace LouisAgent.Evals.Protocol.Events.Run;

/// <summary>
/// The run's first line: the protocol's version, the eval and its version, the commit it was built from, which
/// scenarios it asked for, the run it re-scores (if any), its dataset, its graders and the model each part ran on.
/// </summary>
public sealed record ProtocolEvent(
    DateTimeOffset T,
    int Version,
    string Eval,
    int EvalVersion,
    string Commit,
    RunSplit Split,
    string? RescoreOf,
    DatasetInfo? Dataset,
    IReadOnlyList<GraderInfo> Graders,
    IReadOnlyList<ModelInfo> Models) : EvalEvent(T)
{
    /// <summary>The version this contract writes and reads.</summary>
    public const int CurrentVersion = 2;
}
