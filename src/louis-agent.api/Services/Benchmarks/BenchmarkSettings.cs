using System.Text.Json;

namespace LouisAgent.Api.Services.Benchmarks;

/// <summary>
/// What a run ran with (F31), kept on the run so a later one is compared with it knowing what changed: the instance's
/// provider and model, the personalities it allows, and the check's script and arguments. Not yet the commit: the
/// container has the workspace but no reason to run git for it until a run asks (the feature doc's open point).
/// </summary>
public static class BenchmarkSettings
{
    public static IReadOnlyDictionary<string, string> Of(BenchmarkCheck check, string provider, string model, IReadOnlyList<string> personalities) =>
        new Dictionary<string, string>
        {
            ["provider"] = provider,
            ["model"] = model,
            // An empty list turns every personality on (F29).
            ["personalities_enabled"] = personalities.Count == 0 ? "(all)" : string.Join(",", personalities),
            ["script"] = check.Script,
            ["arguments"] = JsonSerializer.Serialize(check.Arguments),
        };
}
