using System.Text.RegularExpressions;
using LouisAgent.Api.Benchmarks.Runs;

namespace LouisAgent.Api.Tests.Benchmarks.Runs;

// FX25: the runner's process got the API's whole environment (.NET hands a child the parent's by default), so a run could read the Mongo login, the admin key and every
// provider key, of which it reads none. Now it gets an allowlist: what the runtime needs to start, what the runner reads, and what the API sets for the
// run. An Anthropic judge with no key of its own gets the Anthropic key as the judge's key, under that name only. Written first: the start info copied everything.
public class RunnerEnvironmentTests
{
    private static readonly BenchmarkCheck Stub = new BenchmarkCheck("stub", "Stub");

    /// <summary>The API's environment as the containers have it: the runner's settings among the API's secrets.</summary>
    private static readonly Dictionary<string, string> Api = new()
    {
        ["BENCHMARKS_MONGO_URL"] = "mongodb://user:secret@mongo/puls", ["ADMIN_API_KEY"] = "admin-secret", ["ANTHROPIC_API_KEY"] = "sk-ant-secret",
        ["GOOGLE_SEARCH_API_KEY"] = "g-secret", ["PAYMO_API_KEY"] = "p-secret", ["LLM_API_KEY"] = "llm-secret", ["AGENT_API_KEY"] = "agent-key",
        ["EVALS"] = "puls", ["EVAL_JUDGE_PROVIDER"] = "ollama", ["EVAL_JUDGE_MODEL"] = "qwen3:4b", ["EVAL_JUDGE_ENDPOINT"] = "http://ollama:11434",
        ["LOG_DIRECTORY"] = "/app/logs", ["USAGE_LEDGER"] = "on", ["PRICES_FILE"] = "/app/config/prices.json", ["WORKSPACE_ROOT"] = "/workspace",
        ["PATH"] = "/usr/bin", ["HOME"] = "/root", ["DOTNET_ROOT"] = "/usr/share/dotnet", ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1", ["LANG"] = "C.UTF-8",
    };

    [Test]
    public void TheRunnerGetsWhatItReadsAndWhatItNeedsToStart_NotTheAPIsSecrets()
    {
        var env = RunnerEnvironment.Select(Api);

        Assert.That(env.Keys, Is.SupersetOf(new[] { "EVALS", "EVAL_JUDGE_PROVIDER", "EVAL_JUDGE_MODEL", "EVAL_JUDGE_ENDPOINT", "AGENT_API_KEY", "LOG_DIRECTORY", "USAGE_LEDGER", "PRICES_FILE" }), "what the runner reads");
        Assert.That(env.Keys, Is.SupersetOf(new[] { "PATH", "HOME", "DOTNET_ROOT", "DOTNET_CLI_TELEMETRY_OPTOUT", "LANG" }), "what the runtime needs");
        Assert.That(env.Keys, Has.None.AnyOf("BENCHMARKS_MONGO_URL", "ADMIN_API_KEY", "ANTHROPIC_API_KEY", "GOOGLE_SEARCH_API_KEY", "PAYMO_API_KEY", "LLM_API_KEY", "WORKSPACE_ROOT"), "the API's secrets, and what the runner doesn't read");
        Assert.That(env.ContainsKey("EVAL_JUDGE_API_KEY"), Is.False, "a local judge needs no key");
    }

    // Louis, 2026-10-27 (review card): behind a company proxy or a company CA, an Anthropic judge started by the runner can't reach Anthropic if the proxy and certificate settings are dropped.
    // None of them is a secret; they are what the process needs to reach the network, like the path. Names match whatever their case, so one spelling covers the lower-case ones Linux tools use.
    [Test]
    public void TheProxyAndCertificateSettings_ComeThrough_InEitherCase()
    {
        var parent = new Dictionary<string, string>
        {
            ["HTTPS_PROXY"] = "http://proxy:3128", ["HTTP_PROXY"] = "http://proxy:3128", ["NO_PROXY"] = "localhost,ollama", ["SSL_CERT_FILE"] = "/etc/ssl/company.pem", ["SSL_CERT_DIR"] = "/etc/ssl/certs",
            ["https_proxy"] = "http://proxy:3128", ["http_proxy"] = "http://proxy:3128", ["no_proxy"] = "localhost,ollama",
            ["ANTHROPIC_API_KEY"] = "sk-ant-secret",
        };

        var env = RunnerEnvironment.Select(parent);

        Assert.That(env.Keys, Is.EquivalentTo(new[] { "HTTPS_PROXY", "HTTP_PROXY", "NO_PROXY", "SSL_CERT_FILE", "SSL_CERT_DIR", "https_proxy", "http_proxy", "no_proxy" }));
    }

    [Test]
    public void AnAnthropicJudgeWithoutItsOwnKey_GetsTheAnthropicKey_AsTheJudgesKeyOnly()
    {
        var named = new Dictionary<string, string>(Api) { ["EVAL_JUDGE_PROVIDER"] = "anthropic", ["EVAL_JUDGE_MODEL"] = "claude-haiku-5-5" };
        var byModel = new Dictionary<string, string>(Api) { ["EVAL_JUDGE_MODEL"] = "claude-haiku-5-5" };
        byModel.Remove("EVAL_JUDGE_PROVIDER");
        var ownKey = new Dictionary<string, string>(named) { ["EVAL_JUDGE_API_KEY"] = "judge-key" };

        Assert.That(RunnerEnvironment.Select(named)["EVAL_JUDGE_API_KEY"], Is.EqualTo("sk-ant-secret"));
        Assert.That(RunnerEnvironment.Select(byModel)["EVAL_JUDGE_API_KEY"], Is.EqualTo("sk-ant-secret"), "a claude model with no provider named is Anthropic's, as the harness reads it");
        Assert.That(RunnerEnvironment.Select(ownKey)["EVAL_JUDGE_API_KEY"], Is.EqualTo("judge-key"), "a judge key of its own stands");
        Assert.That(RunnerEnvironment.Select(named).ContainsKey("ANTHROPIC_API_KEY"), Is.False, "never under its own name");
    }

    [Test]
    public void NamesAreMatchedWhateverTheirCase_AsWindowsKeepsThem()
    {
        var env = RunnerEnvironment.Select(new Dictionary<string, string> { ["Path"] = "C:\\Windows", ["SystemRoot"] = "C:\\Windows", ["anthropic_api_key"] = "x" });

        Assert.That(env.Keys, Is.EquivalentTo(new[] { "Path", "SystemRoot" }));
    }

    [Test]
    public void TheStartInfo_CarriesTheSelection_AndTheRunsOwnVariables()
    {
        string? mongo = Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_URL");
        Environment.SetEnvironmentVariable("BENCHMARKS_MONGO_URL", "mongodb://user:secret@mongo/puls");
        try
        {
            var start = new BenchmarkRunner(new BenchmarkCatalog([Stub]), TestPaths.RepoRoot, "http://127.0.0.1:1/", RunnerBuild.Dll)
                .StartInfoFor(Stub, new BenchmarkRun("run123456789", "stub", 1));

            Assert.That(start.Environment.ContainsKey("BENCHMARKS_MONGO_URL"), Is.False);
            Assert.That(start.Environment["PULS_API"], Is.EqualTo("http://127.0.0.1:1/"));
            Assert.That(start.Environment["BENCHMARK_RUN"], Is.EqualTo("run123456789"));
            Assert.That(start.Environment.ContainsKey("PATH") || start.Environment.ContainsKey("Path"), Is.True, "the runtime's path comes through");
        }
        finally
        {
            Environment.SetEnvironmentVariable("BENCHMARKS_MONGO_URL", mongo);
        }
    }

    // The review's note on FX25: a setting the runner starts reading must not quietly go missing. This scans the runner's sources (the harness, the runner, Puls's evals) and the Python
    // checks for the names they read, and each must be on the allowlist, except the three the API handles another way: it sets PULS_API and BENCHMARK_RUN per run, and hands the
    // Anthropic key over as EVAL_JUDGE_API_KEY.
    [Test]
    public void EverySettingTheRunnerAndTheChecksRead_IsOnTheAllowlist_OrHandledOnPurpose()
    {
        var handledAnotherWay = new[] { "PULS_API", "BENCHMARK_RUN", "ANTHROPIC_API_KEY" };
        var read = new HashSet<string>();
        foreach (string file in Directory.EnumerateFiles(Path.Combine(TestPaths.RepoRoot, "evals"), "*.cs", SearchOption.AllDirectories)
                     .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")))
            foreach (Match m in Regex.Matches(File.ReadAllText(file), @"(?:GetEnvironmentVariable|getVariable|\bGet)\(""([A-Z][A-Z0-9_]+)""\)"))
                read.Add(m.Groups[1].Value);

        Assert.That(read, Has.Count.GreaterThanOrEqualTo(8), "the scan found the settings the runner reads: " + string.Join(", ", read.Order()));
        Assert.That(read.Except(handledAnotherWay).Where(name => !RunnerEnvironment.Names.Contains(name)), Is.Empty,
            "a setting the runner reads that the allowlist doesn't carry would silently never arrive: add it to RunnerEnvironment.Names");
    }

    /// <summary>The real thing, on a real child process (the stand-in for the runner, F18 step 7.4): what the API starts sees none of the API's secrets, still has the path it needs, and gets what the run is told.</summary>
    [Test]
    public async Task AChildStartedByTheRunner_SeesNoSecret_AndStillStarts()
    {
        string? mongo = Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_URL");
        string? admin = Environment.GetEnvironmentVariable("ADMIN_API_KEY");
        string? anthropic = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        Environment.SetEnvironmentVariable("BENCHMARKS_MONGO_URL", "mongodb://user:secret@mongo/puls");
        Environment.SetEnvironmentVariable("ADMIN_API_KEY", "admin-secret");
        Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", "sk-ant-secret");
        try
        {
            var runner = new BenchmarkRunner(new BenchmarkCatalog([new BenchmarkCheck("env", "Env")]), Path.GetTempPath(), "http://x/", StandInBuild.Dll);
            var run = runner.Start("env", 1)!;
            var lines = new List<string>();
            await foreach (string line in runner.EventsAsync(run.Id)) lines.Add(line);

            Assert.That(run.ExitCode, Is.EqualTo(0), string.Join(" | ", lines));
            var names = lines.Where(l => l.StartsWith("env:")).Select(l => l[4..]).ToList();
            Assert.That(names, Has.None.EqualTo("BENCHMARKS_MONGO_URL").And.None.EqualTo("ADMIN_API_KEY").And.None.EqualTo("ANTHROPIC_API_KEY"), "the API's secrets never reach the child");
            Assert.That(names.Any(n => n.Equals("PATH", StringComparison.OrdinalIgnoreCase)), Is.True, "the path the runtime needs comes through");
            Assert.That(lines, Does.Contain("api=http://x/").And.Contain($"run={run.Id}"), "what the API tells the run arrives");
        }
        finally
        {
            Environment.SetEnvironmentVariable("BENCHMARKS_MONGO_URL", mongo);
            Environment.SetEnvironmentVariable("ADMIN_API_KEY", admin);
            Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", anthropic);
        }
    }
}
