using LouisAgent.Core.Sessions.Replay;

namespace LouisAgent.Evals.Api;

/// <summary>
/// The <c>replay</c> an eval session sends on <c>POST /v1/sessions</c> (F18 step 4a). Every session the runner opens sends one, even with
/// nothing in it: that is the mark of an eval. <paramref name="Web"/> is null when the command line named no <c>--web</c>, so the
/// instance's <c>WEB_REPLAY</c> fills it in. <paramref name="Of"/> is the recorded trial a replay reads, and is null for a recording,
/// which the API resolves to its own <c>{run tag}/{task}</c>. <paramref name="Model"/> asks for the model's answers to replay too
/// (step 4e, <c>--replay</c>); the API accepts it only with a replay.
/// </summary>
public sealed record WebReplayRequest(WebReplayMode? Web, string? Of, bool Model = false);
