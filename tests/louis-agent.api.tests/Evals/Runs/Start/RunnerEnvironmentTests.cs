using System.Text.RegularExpressions;
using LouisAgent.Api.Evals.Runs.Start;

namespace LouisAgent.Api.Tests.Evals.Runs.Start;

/// <summary>
/// The runner's process used to get the API's whole environment, so a run could read the Mongo login, the admin key
/// and every provider key, none of which it needs. Now it gets an allowlist: what the runtime needs to start, what
/// the runner reads, and what the API sets for the run. An Anthropic judge with no key of its own gets the Anthropic
/// key, but only under the judge's name.
/// </summary>
public class RunnerEnvironmentTests
{
    /// <summary>The API's environment as the containers have it: the runner's settings among its secrets.</summary>
    private static readonly Dictionary<string, string> Api = new()
    {
        ["BENCHMARKS_MONGO_URL"] = "mongodb://user:secret@mongo/puls",
        ["ADMIN_API_KEY"] = "admin-secret",
        ["ANTHROPIC_API_KEY"] = "sk-ant-secret",
        ["GOOGLE_SEARCH_API_KEY"] = "g-secret",
        ["PAYMO_API_KEY"] = "p-secret",
        ["LLM_API_KEY"] = "llm-secret",
        ["AGENT_API_KEY"] = "agent-key",
        ["EVALS"] = "puls",
        ["EVAL_JUDGE_PROVIDER"] = "ollama",
        ["EVAL_JUDGE_MODEL"] = "qwen3:4b",
        ["EVAL_JUDGE_ENDPOINT"] = "http://ollama:11434",
        ["LOG_DIRECTORY"] = "/app/logs",
        ["USAGE_LEDGER"] = "on",
        ["PRICES_FILE"] = "/app/config/prices.json",
        ["WORKSPACE_ROOT"] = "/workspace",
        ["PATH"] = "/usr/bin",
        ["HOME"] = "/root",
        ["DOTNET_ROOT"] = "/usr/share/dotnet",
        ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1",
        ["LANG"] = "C.UTF-8",
    };

    /// <summary>
    /// The runner gets what it reads and what the runtime needs to start, and none of the API's secrets; a local
    /// judge gets no key.
    /// </summary>
    [Test]
    public void TheRunnerGetsWhatItReadsAndWhatItNeedsToStart_NotTheAPIsSecrets()
    {
        var env = RunnerEnvironment.Select(Api);

        Assert.That(env.Keys,
            Is.SupersetOf(new[]
            {
                "EVALS", "EVAL_JUDGE_PROVIDER", "EVAL_JUDGE_MODEL", "EVAL_JUDGE_ENDPOINT", "AGENT_API_KEY",
                "LOG_DIRECTORY", "USAGE_LEDGER", "PRICES_FILE"
            }), "what the runner reads");
        Assert.That(env.Keys,
            Is.SupersetOf(new[] { "PATH", "HOME", "DOTNET_ROOT", "DOTNET_CLI_TELEMETRY_OPTOUT", "LANG" }),
            "what the runtime needs");
        Assert.That(env.Keys,
            Has.None.AnyOf("BENCHMARKS_MONGO_URL", "ADMIN_API_KEY", "ANTHROPIC_API_KEY", "GOOGLE_SEARCH_API_KEY",
                "PAYMO_API_KEY", "LLM_API_KEY", "WORKSPACE_ROOT"),
            "the API's secrets, and what the runner doesn't read");
        Assert.That(env.ContainsKey("EVAL_JUDGE_API_KEY"), Is.False, "a local judge needs no key");
    }

    /// <summary>
    /// Proxy and certificate settings come through, in either case. Behind a company proxy or CA, a judge can't reach
    /// Anthropic without them; none is a secret, they're what the process needs to reach the network, like the path.
    /// </summary>
    [Test]
    public void TheProxyAndCertificateSettings_ComeThrough_InEitherCase()
    {
        var parent = new Dictionary<string, string>
        {
            ["HTTPS_PROXY"] = "http://proxy:3128",
            ["HTTP_PROXY"] = "http://proxy:3128",
            ["NO_PROXY"] = "localhost,ollama",
            ["SSL_CERT_FILE"] = "/etc/ssl/company.pem",
            ["SSL_CERT_DIR"] = "/etc/ssl/certs",
            ["https_proxy"] = "http://proxy:3128",
            ["http_proxy"] = "http://proxy:3128",
            ["no_proxy"] = "localhost,ollama",
            ["ANTHROPIC_API_KEY"] = "sk-ant-secret",
        };

        var env = RunnerEnvironment.Select(parent);

        Assert.That(env.Keys,
            Is.EquivalentTo(new[]
            {
                "HTTPS_PROXY", "HTTP_PROXY", "NO_PROXY", "SSL_CERT_FILE", "SSL_CERT_DIR", "https_proxy",
                "http_proxy", "no_proxy"
            }));
    }

    /// <summary>
    /// An Anthropic judge with no key of its own gets the Anthropic key, as the judge's key and never under its own
    /// name; a judge with its own key keeps it.
    /// </summary>
    [Test]
    public void AnAnthropicJudgeWithoutItsOwnKey_GetsTheAnthropicKey_AsTheJudgesKeyOnly()
    {
        // A judge named as Anthropic's, one known only by its Claude model, and one with its own key.
        var named = new Dictionary<string, string>(Api)
        {
            ["EVAL_JUDGE_PROVIDER"] = "anthropic",
            ["EVAL_JUDGE_MODEL"] = "claude-haiku-5-5"
        };
        var byModel = new Dictionary<string, string>(Api) { ["EVAL_JUDGE_MODEL"] = "claude-haiku-5-5" };
        byModel.Remove("EVAL_JUDGE_PROVIDER");
        var ownKey = new Dictionary<string, string>(named) { ["EVAL_JUDGE_API_KEY"] = "judge-key" };

        Assert.That(RunnerEnvironment.Select(named)["EVAL_JUDGE_API_KEY"], Is.EqualTo("sk-ant-secret"));
        Assert.That(RunnerEnvironment.Select(byModel)["EVAL_JUDGE_API_KEY"], Is.EqualTo("sk-ant-secret"),
            "a claude model with no provider named is Anthropic's, as the harness reads it");
        Assert.That(RunnerEnvironment.Select(ownKey)["EVAL_JUDGE_API_KEY"], Is.EqualTo("judge-key"),
            "a judge key of its own stands");
        Assert.That(RunnerEnvironment.Select(named).ContainsKey("ANTHROPIC_API_KEY"), Is.False,
            "never under its own name");
    }

    /// <summary>Names match whatever their case, the way Windows keeps them.</summary>
    [Test]
    public void NamesAreMatchedWhateverTheirCase_AsWindowsKeepsThem()
    {
        var env = RunnerEnvironment.Select(new Dictionary<string, string>
        {
            ["Path"] = "C:\\Windows",
            ["SystemRoot"] = "C:\\Windows",
            ["anthropic_api_key"] = "x"
        });

        Assert.That(env.Keys, Is.EquivalentTo(new[] { "Path", "SystemRoot" }));
    }

    /// <summary>
    /// A setting the runner starts reading must not quietly go missing. Every name the eval sources read must be on
    /// the allowlist, except the ones the API handles itself: it sets the API address and the run id per run (the
    /// runner reads the address under its old name too), and hands the Anthropic key over as the judge's key.
    /// </summary>
    [Test]
    public void EverySettingTheRunnerAndTheChecksRead_IsOnTheAllowlist_OrHandledOnPurpose()
    {
        var handledAnotherWay = new[] { "AGENT_API", "PULS_API", "BENCHMARK_RUN", "ANTHROPIC_API_KEY" };

        // Scan every C# file under evals/ (skipping build output) for the settings it reads by name.
        var read = new HashSet<string>();
        foreach (string file in Directory.EnumerateFiles(Path.Combine(TestPaths.RepoRoot, "evals"), "*.cs",
                         SearchOption.AllDirectories)
                     .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                                 !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")))
            foreach (Match m in Regex.Matches(File.ReadAllText(file),
                         @"(?:GetEnvironmentVariable|getVariable|\bGet)\(""([A-Z][A-Z0-9_]+)""\)"))
                read.Add(m.Groups[1].Value);

        Assert.That(read, Has.Count.GreaterThanOrEqualTo(8),
            "the scan found the settings the runner reads: " + string.Join(", ", read.Order()));
        Assert.That(read.Except(handledAnotherWay).Where(name => !RunnerEnvironment.Names.Contains(name)), Is.Empty,
            "a setting the runner reads that the allowlist doesn't carry would silently never arrive: add it to RunnerEnvironment.Names");
    }
}
