using LouisAgent.Core.Llm.Replay;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Sessions.Replay.Store;
using Microsoft.Extensions.AI;

namespace LouisAgent.Evals.Runs;

/// <summary>
/// Records the judge's answers in a run's trial beside the agent's, and gives them back in a replay, so a replay
/// marks every criterion with no model called and no cost.
/// </summary>
/// <remarks>
/// A decorator over the judge's client: the engine's own replay client, reused whole, with the trial a play is
/// under set around its marks. The judge's answers are replayed too, chosen over replaying the facts only and over a
/// live judge in a replay (Louis, 2026-11-23). A criterion whose words changed asks something the recording doesn't
/// hold, and the miss says the recording is out of date.
/// </remarks>
/// <param name="inner">The judge's model.</param>
/// <param name="store">Where the host keeps its recordings.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0134-an-eval-is-one-file-of-conversations.md">ADR 0134: an eval is one file of conversations</seealso>
public sealed class JudgeReplay(IChatClient inner, ReplayStore store)
    : DelegatingChatClient(new ModelReplayChatClient(inner, store))
{
    /// <summary>
    /// The judge's calls from here on record into this trial, or replay from the recorded one, until it is disposed.
    /// </summary>
    public static IDisposable Begin(RunTrial trial) => SessionReplay.Use(trial.Replayed is { } recorded
        ? new SessionReplay(WebReplayMode.Replay, recorded, Model: true)
        : new SessionReplay(WebReplayMode.Record, trial.Name, Model: false));
}
