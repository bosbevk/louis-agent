using LouisAgent.Api.Evals.Testing;
using LouisAgent.Core.Anthropic;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Web;
using LouisAgent.Evals.Files.Json;
using LouisAgent.Identities.Triage;

namespace LouisAgent.Coding.Tests.Evals;

/// <summary>
/// The triage manager's eval, replayed from the recordings kept in git (F126.5.3): every fact of every turn is met,
/// the routing facts among them, with no key and no model. The roles' tools are played back too (FX235), so the
/// replay reads nothing of the sample and writes no run of its own anywhere but a temp folder.
/// </summary>
public class TriageEvalReplayTests
{
    /// <summary>Where the triage team's eval recordings are kept.</summary>
    private static readonly string Recordings =
        Path.Combine(TestPaths.RepoRoot, "demos", "louis-agent", "recordings", "evals", "triage");

    /// <summary>A temp folder for what the triage pack keeps, so a replay writes nothing in the repo.</summary>
    private static readonly Lazy<string> Kept = new(() =>
        Directory.CreateTempSubdirectory("triage-replay-").FullName);

    /// <summary>
    /// The settings the demo's API container runs the team with (its compose service and the tracked model settings),
    /// with the pack's folders in a temp folder and keys that are never used, since nothing calls a model.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> DemoSettings = new Dictionary<string, string>
    {
        ["LLM_PROVIDER"] = "anthropic",
        ["LLM_MODEL"] = "claude-haiku-5-5",
        ["ANTHROPIC_API_KEY"] = "replay-only-never-sent",
        ["WORKSPACE_ROOT"] = TestPaths.RepoRoot,
        ["DOCS_ROOT"] = TestPaths.RepoRoot,
        ["LEADS_TO"] = "louismarais0@gmail.com",
        ["LEADS_FROM"] = "guide@louis-agent.dev",
        ["TRIAGE_RUNS"] = Path.Combine(Kept.Value, "runs"),
        ["TRIAGE_PEOPLE"] = Path.Combine(Kept.Value, "people"),
        ["TRIAGE_DEPARTMENTS"] = Path.Combine(TestPaths.RepoRoot, "demos", "louis-agent", "config", "departments.yaml"),
        ["OPEN_IDENTITIES"] = "louis-agent-guide",
        ["AGENT_PEERS"] = "self=http://localhost:8080",
        ["OPEN_SESSIONS_PER_DAY"] = "50",
        ["PRICES_FILE"] = Path.Combine(TestPaths.RepoRoot, "demos", "louis-agent", "config", "prices.json"),
        ["RATES_FILE"] = Path.Combine(TestPaths.RepoRoot, "demos", "louis-agent", "config", "rates.json")
    };

    /// <summary>What the demo's program composes besides its catalogue: the web and the provider the team runs on.</summary>
    private static readonly HostServices Services = new(WebAccess.Create, Providers: [AnthropicProvider.Instance]);

    /// <summary>
    /// Every fact of every turn of the triage manager's eval is met in the replay, and every conversation of the eval
    /// is replayed, so a conversation with no recording fails here.
    /// </summary>
    [Test]
    public async Task TheTriageEval_ReplayedFromItsRecordings_MeetsEveryFact_InEveryConversation()
    {
        var replayed = await EvalReplay.AssertFactsMetAsync(CodingCatalog.Create, Services, DemoSettings, "triage",
            Recordings);

        using var file = new StreamReader(new TriageIdentity().Prompts.GetFileInfo("agents/triage/triage.eval.json")
            .CreateReadStream());
        var every = EvalFileReader.Read(file.ReadToEnd()).Conversations.Select(conversation => conversation.Id);
        Assert.That(replayed, Is.EqualTo(every));
    }
}
