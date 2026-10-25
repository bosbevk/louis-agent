using LouisAgent.Api.Messages;
using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Sessions.Replay;

namespace LouisAgent.Api.Sessions.Requests;

/// <summary>
/// Turns a session's <c>replay</c> and the instance's settings into what the session does with the web (F18 step 4a; the runner only
/// asks, the API decides: ADR 0029), or into the reason it refuses (a 400 before any engine is built). <b>An eval marks itself by
/// sending <c>replay</c>, even an empty <c>{}</c>; a run tag does not</b> (the orchestrator sends one on every fix session): with no
/// <c>replay</c> the session is left alone, whatever <c>WEB_REPLAY</c> says, and a child sent none (its parent said off) stays live. The
/// rules, in order: a <c>replay</c> with no run tag is refused (a live chat is never recorded); <c>web</c> is parsed with
/// <c>SettingChoice</c>, and the instance default only fills in a missing one; <c>model: true</c> is for web <c>replay</c> only (step 4e: the
/// model's answers come from a recording, with its web) and is refused with any other mode, <c>off</c> included; <c>off</c> is none; record or replay needs an instance that keeps recordings (<c>AgentOptions.KeepsRecordings</c>); replay
/// needs <c>of</c>; a recording without one writes <c>{run}/{task}</c>, and one with one (a sub-session, sent its parent's) keeps it, so
/// a child never works out a key of its own.
/// </summary>
internal static class ReplayResolution
{
    public static bool TryResolve(CreateSessionRequest? request, WebReplayMode defaultMode, bool keepsRecordings, out SessionReplay? replay,
        out string? refusal)
    {
        replay = null;
        refusal = null;
        if (request?.Replay is not { } sent) return true;
        if (Clean(request.Run) is not { } run) return Refuse(ApiUserText.ReplayNeedsRunTag, null, out refusal);

        if (!SettingChoice.TryParse(sent.Web, out WebReplayMode? choice))
            return Refuse(ApiUserText.UnknownReplayWeb,
                new { Value = sent.Web!.Trim(), Supported = string.Join(", ", SettingChoice.Names<WebReplayMode>()) }, out refusal);

        var mode = choice ?? defaultMode;
        if (sent.Model && mode != WebReplayMode.Replay) return Refuse(ApiUserText.ReplayModelNeedsReplay, null, out refusal);
        if (mode == WebReplayMode.Off) return true;
        if (!keepsRecordings) return Refuse(ApiUserText.ReplayNoRecordings, null, out refusal);

        string? of = Clean(sent.Of);
        if (mode == WebReplayMode.Replay && of is null) return Refuse(ApiUserText.ReplayNeedsOf, null, out refusal);
        of ??= Clean(request.Task) is { } task ? $"{run}/{task}" : run;
        replay = new SessionReplay(mode, of, sent.Model);
        return true;
    }

    /// <summary>
    /// The trial a recording starts afresh (F18 step 5a), or null. The runner runs a trial once more after a transport failure, under the same key, and the
    /// replay store appends and answers from an ask's first recording, so a top-level eval session that records clears what an earlier attempt wrote. A
    /// sub-session (it is sent its parent's <c>of</c>) records into its parent's trial and must never clear it.
    /// </summary>
    public static string? FreshTrial(CreateSessionRequest? request, SessionReplay? replay) =>
        replay is { Web: WebReplayMode.Record, Of: { } trial } && Clean(request?.Replay?.Of) is null ? trial : null;

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private static bool Refuse(ApiUserText message, object? values, out string? refusal)
    {
        refusal = MessageText.Format(message, values);
        return false;
    }
}
