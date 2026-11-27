using LouisAgent.Api.Evals.Testing;
using LouisAgent.Core.Anthropic;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Web;
using LouisAgent.Evals.Files.Json;
using LouisAgent.Identities.Coding;

namespace LouisAgent.Coding.Tests.Evals;

/// <summary>
/// The coding agent's eval, replayed from the recordings kept in git (F97.3): every fact of every turn is met, the
/// tool each job needs among them, with no key and no model. Its tools are played back too (FX235), so the replay
/// reads nothing of the repository as it is now.
/// </summary>
public class CodingEvalReplayTests
{
    /// <summary>Where the coding agent's eval recordings are kept.</summary>
    private static readonly string Recordings =
        Path.Combine(TestPaths.RepoRoot, "demos", "louis-agent", "recordings", "evals", "coding");

    /// <summary>
    /// The settings the demo's API container runs the coding agent with (its compose service and the tracked model
    /// settings), with keys that are never used, since nothing calls a model.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> DemoSettings = new Dictionary<string, string>
    {
        ["LLM_PROVIDER"] = "anthropic",
        ["LLM_MODEL"] = "claude-haiku-5-5",
        ["ANTHROPIC_API_KEY"] = "replay-only-never-sent",
        ["WORKSPACE_ROOT"] = TestPaths.RepoRoot,
        ["DOCS_ROOT"] = TestPaths.RepoRoot,
        ["AGENT_PEERS"] = "self=http://localhost:8080",
        ["PRICES_FILE"] = Path.Combine(TestPaths.RepoRoot, "demos", "louis-agent", "config", "prices.json"),
        ["RATES_FILE"] = Path.Combine(TestPaths.RepoRoot, "demos", "louis-agent", "config", "rates.json")
    };

    /// <summary>What the demo's program composes besides its catalogue: the web and the agent's provider.</summary>
    private static readonly HostServices Services = new(WebAccess.Create, Providers: [AnthropicProvider.Instance]);

    /// <summary>
    /// Every fact of every turn of the coding agent's eval is met in the replay, and every conversation of the eval is
    /// replayed, so a conversation with no recording fails here.
    /// </summary>
    [Test]
    public async Task TheCodingEval_ReplayedFromItsRecordings_MeetsEveryFact_InEveryConversation()
    {
        // The catalogue the project's prompts were registered from, as the triage replay uses (a second one would
        // bring its prompts again, which the engine refuses).
        var replayed = await EvalReplay.AssertFactsMetAsync(() => CodingPrompts.Catalog, Services, DemoSettings,
            "coding", Recordings);

        using var file = new StreamReader(new CodingIdentity().Prompts
            .GetFileInfo("agents/louis-agent/coding.eval.json").CreateReadStream());
        var every = EvalFileReader.Read(file.ReadToEnd()).Conversations.Select(conversation => conversation.Id);
        Assert.That(replayed, Is.EqualTo(every));
    }
}
