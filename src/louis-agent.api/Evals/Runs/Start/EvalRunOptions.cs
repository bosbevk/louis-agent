using LouisAgent.Api.Messages;
using LouisAgent.Choices;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Sessions.Replay;

namespace LouisAgent.Api.Evals.Runs.Start;

/// <summary>
/// How a run treats the web, as the portal's start request asks. <c>{"repeat": 1, "web": "record"}</c> records every
/// page, search and fetch the run reads; <c>{"repeat": 1, "replay": "{runId}"}</c> plays a recorded run back, the
/// model's answers too, so it costs nothing. The runner gets them as <c>--web record</c> and <c>--replay {runId}</c>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public sealed record EvalRunOptions(WebReplayMode? Web = null, string? Replay = null)
{
    /// <summary>A run that asks for nothing: the instance's own <c>WEB_REPLAY</c> decides.</summary>
    public static readonly EvalRunOptions None = new();

    /// <summary>
    /// Reads what the start request asked for. If it can't be done, you get the reason to send back as a 400 instead.
    /// </summary>
    /// <remarks>
    /// Guard clauses (Fowler, <i>Replace Nested Conditional with Guard Clauses</i>): one check per way a request can
    /// go, from the top, so each refusal reads on its own line.
    /// </remarks>
    public static (EvalRunOptions? Options, string? Refusal) Parse(string? web, string? replay)
    {
        web = web?.Trim();
        replay = replay?.Trim();

        // Nothing asked, or both at once: the first is fine, the second makes no sense.
        if (string.IsNullOrEmpty(web) && string.IsNullOrEmpty(replay)) return (None, null);
        if (!string.IsNullOrEmpty(web) && !string.IsNullOrEmpty(replay))
            return (null, MessageText.Format(ApiUserText.RunWebAndReplay));

        // A replay names a run id, so only letters, digits, '_' and '-' get through.
        if (!string.IsNullOrEmpty(replay))
            return replay.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-')
                ? (new EvalRunOptions(null, replay), null)
                : (null, MessageText.Format(ApiUserText.RunBadReplay, new { Value = replay }));

        // A web mode must be one we know, and "replay" has its own field because it needs a run to play.
        if (!SettingChoice.TryParse(web, out WebReplayMode? choice) || choice is null)
            return (null, MessageText.Format(ApiUserText.RunBadWeb, new
            {
                Value = web,
                Supported = Supported
            }));
        return choice == WebReplayMode.Replay
            ? (null, MessageText.Format(ApiUserText.RunWebIsReplay))
            : (new EvalRunOptions(choice, null), null);
    }

    /// <summary>What <c>web</c> accepts: the modes but replay, which is its own field because it names a run.</summary>
    private static string Supported =>
        string.Join(", ", SettingChoice.Names<WebReplayMode>().Where(name => name != "replay"));
}
