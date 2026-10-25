using LouisAgent.Api.Messages;
using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Sessions.Replay;

namespace LouisAgent.Api.Sessions.Requests;

/// <summary>
/// Turns a session's <c>replay</c> and the instance's settings into what the session does with the web (F18 step 4a; the runner only
/// asks, the API decides: ADR 0029), or into the reason it refuses (a 400 before any engine is built). The rules, in order: no run tag
/// is never recorded and a <c>replay</c> sent is refused; with a run tag and none sent, the default (<c>WEB_REPLAY</c>); <c>web</c> is
/// parsed with <c>SettingChoice</c>; <c>model: true</c> is refused until step 4e, whatever the mode; <c>off</c> is none; record or
/// replay needs an instance that keeps recordings (<c>AgentOptions.KeepsRecordings</c>); replay needs <c>of</c>; a recording without one writes <c>{run}/{task}</c>, and one with one
/// (a sub-session, sent its parent's) keeps it, so a child never works out a key of its own.
/// </summary>
internal static class ReplayResolution
{
    public static bool TryResolve(CreateSessionRequest? request, WebReplayMode defaultMode, bool keepsRecordings, out SessionReplay? replay,
        out string? refusal)
    {
        replay = null;
        refusal = null;
        var sent = request?.Replay;
        if (Clean(request?.Run) is not { } run)
            return sent is null || Refuse(ApiUserText.ReplayNeedsRunTag, null, out refusal);

        if (!SettingChoice.TryParse(sent?.Web, out WebReplayMode? choice))
            return Refuse(ApiUserText.UnknownReplayWeb,
                new { Value = sent!.Web!.Trim(), Supported = string.Join(", ", SettingChoice.Names<WebReplayMode>()) }, out refusal);
        if (sent?.Model == true) return Refuse(ApiUserText.ReplayModelNotYet, null, out refusal);

        var mode = choice ?? defaultMode;
        if (mode == WebReplayMode.Off) return true;
        if (!keepsRecordings) return Refuse(ApiUserText.ReplayNoRecordings, null, out refusal);

        string? of = Clean(sent?.Of);
        if (mode == WebReplayMode.Replay && of is null) return Refuse(ApiUserText.ReplayNeedsOf, null, out refusal);
        of ??= Clean(request!.Task) is { } task ? $"{run}/{task}" : run;
        replay = new SessionReplay(mode, of, Model: false);
        return true;
    }

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private static bool Refuse(ApiUserText message, object? values, out string? refusal)
    {
        refusal = MessageText.Format(message, values);
        return false;
    }
}
