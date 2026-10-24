using System.Text.Json;

namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>
/// What a run ran with (F31), kept on the run so a later one is compared with it knowing what changed: the instance's provider and model, the
/// roles it allows, and what the check ran: a Python check's script and arguments, or an eval's name and the runner it started from (F18 step
/// 4.3). Not yet the commit: the container has the workspace but no reason to run git for it until a run asks (the feature doc's open point).
/// </summary>
public static class BenchmarkSettings
{
    public static IReadOnlyDictionary<string, string> Of(BenchmarkCheck check, string provider, string model, IReadOnlyList<string> roles, string? evalRunner = null)
    {
        var settings = new Dictionary<string, string>
        {
            ["provider"] = provider,
            ["model"] = model,
            // An empty list turns every role on (F29); runs before CL14 kept this as personalities_enabled.
            ["roles_enabled"] = roles.Count == 0 ? "(all)" : string.Join(",", roles),
        };
        if (check.Kind == BenchmarkCheckKind.Eval)
        {
            ArgumentNullException.ThrowIfNull(evalRunner);
            settings["eval"] = check.Id;
            settings["eval_runner"] = evalRunner;
        }
        else
        {
            settings["script"] = check.Script;
            settings["arguments"] = JsonSerializer.Serialize(check.Arguments);
        }
        return settings;
    }
}
