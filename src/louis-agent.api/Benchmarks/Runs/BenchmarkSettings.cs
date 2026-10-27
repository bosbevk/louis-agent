using LouisAgent.Choices;
namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>
/// What a run ran with (F31), kept on the run so a later one is compared with it knowing what changed: the instance's provider and model, the
/// roles it allows, and what the check ran: the eval's name and the runner it started from (F18 step
/// 4.3; a script and its arguments until step 7.4). Not yet the commit: the container has the workspace but no reason to run git for it until a run asks (the feature doc's open point).
/// </summary>
public static class BenchmarkSettings
{
    /// <param name="evalRunner">The runner's path the API starts an eval check from. Required, so a caller can't leave it out.</param>
    public static IReadOnlyDictionary<string, string> Of(BenchmarkCheck check, string provider, string model, IReadOnlyList<string> roles, string evalRunner)
    {
        var settings = new Dictionary<string, string>
        {
            ["provider"] = provider,
            ["model"] = model,
            // An empty list turns every role on (F29); runs before CL14 kept this as personalities_enabled.
            ["roles_enabled"] = roles.Count == 0 ? "(all)" : string.Join(",", roles),
        };
        settings["eval"] = check.Id;
        settings["eval_runner"] = evalRunner;
        return settings;
    }

    /// <summary>
    /// The settings plus how the run's web was handled (F18 step 5.5): <c>web</c> when it asked for one, <c>replay_of</c> when it replayed another run, which the
    /// trend uses to leave a replay out (it is the recorded run again, at no cost). As given when it asked for nothing.
    /// </summary>
    public static IReadOnlyDictionary<string, string> WithOptions(IReadOnlyDictionary<string, string> settings, BenchmarkRunOptions options)
    {
        if (options.Web is null && options.Replay is null) return settings;
        var withOptions = new Dictionary<string, string>(settings);
        if (options.Web is { } web) withOptions["web"] = web.Wire();
        if (options.Replay is { } replayOf) withOptions["replay_of"] = replayOf;
        return withOptions;
    }

    /// <summary>
    /// The settings plus <c>prompt_hashes</c>, the run's hashes joined with commas (F18 step 4c); as given, with no key, when there are
    /// none (absent, never empty). <b>It is the set of prompts the run used, not what it has.</b> A run that never called the cook, or
    /// never summarised, lacks those agents' hashes, so two runs with different lists prove nothing by themselves: the difference may be
    /// a changed prompt or only a different path through the same eval. Equal lists show the same prompts; to compare a prompt change,
    /// compare per agent (step 7a), from the ledger's rows.
    /// </summary>
    public static IReadOnlyDictionary<string, string>? WithPromptHashes(IReadOnlyDictionary<string, string>? settings, IReadOnlyList<string> hashes) =>
        hashes.Count == 0 ? settings : new Dictionary<string, string>(settings ?? new Dictionary<string, string>()) { ["prompt_hashes"] = string.Join(",", hashes) };
}
