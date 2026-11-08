namespace LouisAgent.Packs.Triage;

/// <summary>
/// The triage pack's one setting, read once by the composition root: where a run's steps are kept. Unset, the pack
/// stays off.
/// </summary>
/// <remarks>
/// A plain options object with a reader that takes the variable lookup, as <c>DocsOptions</c> is, so a test needs
/// no real environment.
/// </remarks>
public sealed class TriageOptions
{
    /// <summary>The setting's name, which the toolset reports as missing while it is unset.</summary>
    public const string RunsSetting = "TRIAGE_RUNS";

    /// <summary>TRIAGE_RUNS: the folder a run's steps are written under, one file per run.</summary>
    public string? Runs { get; init; }

    /// <summary>Reads the setting; the variable lookup is replaceable for tests.</summary>
    public static TriageOptions FromEnvironment(Func<string, string?>? getVariable = null)
    {
        getVariable ??= Environment.GetEnvironmentVariable;
        string? runs = getVariable(RunsSetting);
        return new TriageOptions { Runs = string.IsNullOrWhiteSpace(runs) ? null : runs.Trim() };
    }
}
