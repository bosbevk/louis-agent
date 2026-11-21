using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Evals.Running;

/// <summary>
/// One run of an eval: its id (the tag its sessions carry, so the ledger's rows find it) and how many times each mode
/// repeats. With <paramref name="Web"/> (<c>--web</c>) the run records its web input, or replays it;
/// <paramref name="Of"/> is then the recorded run's <em>id</em> (not a trial's key: the runner builds each trial's key
/// from it), set only for a replay. <paramref name="Split"/> (<c>--split</c>) picks the scenarios: the dev ones by
/// default.
/// </summary>
public sealed record EvalRun(
    string RunId,
    int Repeats,
    WebReplayMode? Web = null,
    string? Of = null,
    bool Model = false,
    RunSplit Split = RunSplit.Dev);
