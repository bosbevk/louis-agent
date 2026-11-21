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

    /// <summary>The second setting's name: where a case raised to a person goes.</summary>
    public const string PeopleSetting = "TRIAGE_PEOPLE";

    /// <summary>TRIAGE_RUNS: the folder a run's steps are written under, one file per run.</summary>
    public string? Runs { get; init; }

    /// <summary>The third setting's name: the departments file the sorter's choice is checked against.</summary>
    public const string DepartmentsSetting = "TRIAGE_DEPARTMENTS";

    /// <summary>
    /// TRIAGE_PEOPLE: the folder a case raised to a person is written in, one file per run, until a department's
    /// own endpoint names where its people read (a tracker, a mailbox).
    /// </summary>
    public string? People { get; init; }

    /// <summary>TRIAGE_DEPARTMENTS: the departments file (F106.3.2), whose names the sorter may send a case to.</summary>
    public string? Departments { get; init; }

    /// <summary>Reads the settings; the variable lookup is replaceable for tests.</summary>
    public static TriageOptions FromEnvironment(Func<string, string?>? getVariable = null)
    {
        getVariable ??= Environment.GetEnvironmentVariable;
        return new TriageOptions
        {
            Runs = Trimmed(getVariable(RunsSetting)),
            People = Trimmed(getVariable(PeopleSetting)),
            Departments = Trimmed(getVariable(DepartmentsSetting))
        };
    }

    /// <summary>A setting's value, or none when it is empty.</summary>
    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
