using LouisAgent.Api.Messages;
using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Sessions.Replay;

namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>
/// How a run handles the web (F18 step 5.5), as the portal's start request says it: <c>{"repeat": 1, "web": "record"}</c> records every page, search and
/// fetch the run reads, and <c>{"repeat": 1, "replay": "{runId}"}</c> replays a recorded run, the model's answers too, so it costs nothing (the runner's
/// <c>--web record</c> and <c>--replay {runId}</c>). Parsed once where it enters (ADR 0024). (Every check is an eval since F18 step 7.4.)
/// </summary>
public sealed record BenchmarkRunOptions(WebReplayMode? Web = null, string? Replay = null)
{
    /// <summary>A run that asks for nothing: the instance's own <c>WEB_REPLAY</c> decides.</summary>
    public static readonly BenchmarkRunOptions None = new();

    /// <summary>The options a request asks for, or the refusal's text (a 400's message) when it can't be what a run does.</summary>
    public static (BenchmarkRunOptions? Options, string? Refusal) Parse(string? web, string? replay)
    {
        web = web?.Trim();
        replay = replay?.Trim();
        if (string.IsNullOrEmpty(web) && string.IsNullOrEmpty(replay)) return (None, null);
        if (!string.IsNullOrEmpty(web) && !string.IsNullOrEmpty(replay)) return (null, MessageText.Format(ApiUserText.RunWebAndReplay));

        if (!string.IsNullOrEmpty(replay))
            return replay.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-')
                ? (new BenchmarkRunOptions(null, replay), null)
                : (null, MessageText.Format(ApiUserText.RunBadReplay, new { Value = replay }));

        if (!SettingChoice.TryParse(web, out WebReplayMode? choice) || choice is null)
            return (null, MessageText.Format(ApiUserText.RunBadWeb, new { Value = web, Supported = Supported }));
        return choice == WebReplayMode.Replay
            ? (null, MessageText.Format(ApiUserText.RunWebIsReplay))
            : (new BenchmarkRunOptions(choice, null), null);
    }

    /// <summary>What <c>web</c> accepts: the modes but replay, which is its own field because it names a run.</summary>
    private static string Supported => string.Join(", ", SettingChoice.Names<WebReplayMode>().Where(name => name != "replay"));
}
