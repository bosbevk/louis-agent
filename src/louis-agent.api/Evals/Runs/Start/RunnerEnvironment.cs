namespace LouisAgent.Api.Evals.Runs.Start;

/// <summary>
/// Decides which of the API's settings a run's process gets to see. Left alone, a child process inherits everything,
/// so the runner could read the Mongo login, the admin key and every provider key, and it needs none of them.
/// </summary>
/// <remarks>
/// An allowlist. <see cref="Select"/> keeps three things: what the runtime needs to start, what the eval runner reads,
/// and the judge's key when the judge is Anthropic's and has none of its own (then <c>ANTHROPIC_API_KEY</c> travels as
/// <c>EVAL_JUDGE_API_KEY</c>, under the judge's name, never the API's). Names match whatever their case, since Windows
/// spells it <c>Path</c>. A blocklist would let every new secret through by default; with this one, a setting the
/// runner starts reading has to be added to <see cref="Names"/>, or it never arrives.
/// </remarks>
internal static class RunnerEnvironment
{
    /// <summary>
    /// What the process needs to start at all: the path, a home, a temp folder, the locale, and Windows's own; and what
    /// it needs to reach the network (Louis, 2026-10-27): the proxy and certificate settings, so an Anthropic judge
    /// works behind a company proxy or CA. None is a secret. The names match whatever their case, so
    /// <c>https_proxy</c> is covered too.
    /// </summary>
    private static readonly string[] Runtime =
    [
        "PATH", "PATHEXT", "HOME", "USERPROFILE", "APPDATA", "LOCALAPPDATA", "TEMP", "TMP", "TMPDIR", "SystemRoot",
        "SystemDrive", "ComSpec", "LANG", "LC_ALL", "TZ",
        "HTTPS_PROXY", "HTTP_PROXY", "NO_PROXY", "SSL_CERT_FILE", "SSL_CERT_DIR"
    ];

    /// <summary>
    /// What the eval runner reads itself, and through core for the judge's ledger rows; a Python check reads
    /// <c>AGENT_API_KEY</c> of these.
    /// </summary>
    private static readonly string[] Runner =
    [
        "EVALS", "CI", "EVAL_JUDGE_PROVIDER", "EVAL_JUDGE_MODEL", "EVAL_JUDGE_ENDPOINT", "EVAL_JUDGE_API_KEY",
        "AGENT_API_KEY", "LOG_DIRECTORY", "USAGE_LEDGER", "PRICES_FILE", "PRICES_URL"
    ];

    /// <summary>Every name the process may see; <see cref="Prefixes"/> add the runtime's own family.</summary>
    public static readonly IReadOnlySet<string> Names =
        new HashSet<string>([.. Runtime, .. Runner], StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The runtime's own settings (<c>DOTNET_ROOT</c>, <c>DOTNET_CLI_TELEMETRY_OPTOUT</c>…), whatever their suffix.
    /// </summary>
    public static readonly string[] Prefixes = ["DOTNET_"];

    private const string JudgeKey = "EVAL_JUDGE_API_KEY", AnthropicKey = "ANTHROPIC_API_KEY";

    /// <summary>
    /// The part of the API's settings the run may see, with the judge's key sorted out. Names keep the API's spelling.
    /// </summary>
    public static Dictionary<string, string> Select(IReadOnlyDictionary<string, string> parent)
    {
        // Ordinal: on Linux HTTPS_PROXY and https_proxy are two variables and both must arrive; the allowlist's own
        // match ignores the case.
        var selected = new Dictionary<string, string>();
        foreach (var (name, value) in parent)
            if (Names.Contains(name) || Prefixes.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                selected[name] = value;

        // An Anthropic judge with no key of its own uses the API's Anthropic key (as the harness's JudgeSettings falls
        // back): hand it over under the judge's name only.
        if (!selected.Keys.Any(k => k.Equals(JudgeKey, StringComparison.OrdinalIgnoreCase)) &&
            Get(parent, AnthropicKey) is { Length: > 0 } key && JudgeIsAnthropic(parent)) selected[JudgeKey] = key;
        return selected;
    }

    /// <summary>
    /// Whether the judge is one of Anthropic's: it says so, or it names no provider and its model is a Claude. Same
    /// rule as the harness's <c>JudgeSettings</c>.
    /// </summary>
    private static bool JudgeIsAnthropic(IReadOnlyDictionary<string, string> parent)
    {
        string? provider = Get(parent, "EVAL_JUDGE_PROVIDER")?.Trim();
        string? model = Get(parent, "EVAL_JUDGE_MODEL")?.Trim();

        // A named provider decides on its own; only without one does the model's name count.
        if (!string.IsNullOrEmpty(provider)) return provider.Equals("anthropic", StringComparison.OrdinalIgnoreCase);
        return model is not null && model.StartsWith("claude", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A setting's value, whatever the case of its name.</summary>
    private static string? Get(IReadOnlyDictionary<string, string> parent, string name) =>
        parent.FirstOrDefault(p => p.Key.Equals(name, StringComparison.OrdinalIgnoreCase)).Value;
}
