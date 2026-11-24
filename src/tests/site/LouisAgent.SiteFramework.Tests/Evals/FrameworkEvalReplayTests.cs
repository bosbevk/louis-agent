using LouisAgent.Api.Evals.Testing;
using LouisAgent.Core.Anthropic;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Web;
using LouisAgent.Evals.Files.Json;
using LouisAgent.Identities.Framework;
using LouisAgent.SiteFrameworkApi;

namespace LouisAgent.SiteFramework.Tests.Evals;

/// <summary>
/// The framework Orbi's eval, replayed from the recordings kept in git (F126.5.2): every fact of every turn is met,
/// with no key and no model, and over no docs, since the tools' answers are played back too (FX235). A change to its
/// prompt or its tools asks the recording something it doesn't hold, and this turns red until a live run records
/// again.
/// </summary>
public class FrameworkEvalReplayTests
{
    /// <summary>Where the framework Orbi's eval recordings are kept, a folder of its own beside the guide's.</summary>
    private static readonly string Recordings =
        Path.Combine(TestPaths.RepoRoot, "src", "site", "recordings", "evals", "framework");

    /// <summary>A docs folder with nothing in it: the replay must never need a doc.</summary>
    private static readonly Lazy<string> NoDocs = new(() =>
        Directory.CreateTempSubdirectory("framework-no-docs-").FullName);

    /// <summary>
    /// The settings the framework site's container runs Orbi with (its compose service and the tracked model
    /// settings), with an empty docs folder and a key that is never used, since nothing calls a model.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> FrameworkSettings = new Dictionary<string, string>
    {
        ["LLM_PROVIDER"] = "anthropic",
        ["LLM_MODEL"] = "claude-haiku-5-5",
        ["ANTHROPIC_API_KEY"] = "replay-only-never-sent",
        ["STREAM_PACE"] = "on",
        ["OPEN_IDENTITIES"] = "louis-agent-framework",
        ["OPEN_SESSIONS_PER_DAY"] = "20",
        ["DOCS_ROOT"] = NoDocs.Value,
        ["PRICES_FILE"] = Path.Combine(TestPaths.RepoRoot, "src", "site", "config", "prices.json"),
        ["RATES_FILE"] = Path.Combine(TestPaths.RepoRoot, "src", "site", "config", "rates.json")
    };

    /// <summary>What the framework site's program composes besides its catalogue: the web and one provider.</summary>
    private static readonly HostServices Services = new(WebAccess.Create, Providers: [AnthropicProvider.Instance]);

    /// <summary>
    /// Every fact of every turn of the framework Orbi's eval is met in the replay, and every conversation of the eval
    /// is replayed, so a conversation with no recording fails here.
    /// </summary>
    [Test]
    public async Task TheFrameworksEval_ReplayedFromItsRecordings_MeetsEveryFact_InEveryConversation_OverNoDocs()
    {
        var replayed = await EvalReplay.AssertFactsMetAsync(FrameworkCatalog.Create, Services, FrameworkSettings,
            "framework", Recordings);

        using var file = new StreamReader(new FrameworkIdentity().Prompts
            .GetFileInfo("agents/louis-agent-framework/framework.eval.json").CreateReadStream());
        var every = EvalFileReader.Read(file.ReadToEnd()).Conversations.Select(conversation => conversation.Id);
        Assert.That(replayed, Is.EqualTo(every));
    }
}
