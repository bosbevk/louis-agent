using LouisAgent.Api.Evals.Testing;
using LouisAgent.Core.Anthropic;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Web;
using LouisAgent.Evals.Files.Json;
using LouisAgent.Identities.Guide;
using LouisAgent.SiteApi;

namespace LouisAgent.Site.Tests.Guide.Evals;

/// <summary>
/// The guide's eval, replayed from the recordings kept in git: every fact of every turn is met, with no key and no
/// model. A change to the guide's prompt or its tools asks the recording something it doesn't hold, and this turns red
/// until a live run records again. A change to what a tool reads doesn't: the tools' answers are played back too
/// (FX235).
/// </summary>
public class GuideEvalReplayTests
{
    /// <summary>Where the guide's eval recordings are kept, a folder of its own beside each other eval's.</summary>
    private static readonly string Recordings =
        Path.Combine(TestPaths.RepoRoot, "src", "site", "recordings", "evals", "guide");

    /// <summary>
    /// A docs folder with nothing in it: the replay plays back what the docs tools answered on the live run, so the
    /// guide must never need a doc here (FX235: a docs edit once turned this red).
    /// </summary>
    private static readonly Lazy<string> NoDocs = new(() =>
        Directory.CreateTempSubdirectory("guide-no-docs-").FullName);

    /// <summary>
    /// The settings the site's container runs the guide with (its compose file and its tracked model settings), with
    /// an empty docs folder and a key that is never used, since nothing calls a model.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> SiteSettings = new Dictionary<string, string>
    {
        ["LLM_PROVIDER"] = "anthropic",
        ["LLM_MODEL"] = "claude-haiku-5-5",
        ["ANTHROPIC_API_KEY"] = "replay-only-never-sent",
        ["HOST_TIME_ZONE"] = "Africa/Johannesburg",
        ["HOST_CULTURE"] = "en-ZA",
        ["STREAM_PACE"] = "on",
        ["LEADS_TO"] = "louismarais0@gmail.com",
        ["LEADS_FROM"] = "guide@louis-agent.dev",
        ["RESEND_API_KEY"] = "replay-only-never-sent",
        ["OPEN_IDENTITIES"] = "louis-agent-guide,seemeeu-guesthouse,wheatfield-kitchen,koringland-attorneys",
        ["OPEN_SESSIONS_PER_DAY"] = "50",
        ["AGENT_PEERS"] = "self=http://localhost:8080",
        ["DOCS_ROOT"] = NoDocs.Value,
        ["PRICES_FILE"] = Path.Combine(TestPaths.RepoRoot, "src", "site", "config", "prices.json"),
        ["RATES_FILE"] = Path.Combine(TestPaths.RepoRoot, "src", "site", "config", "rates.json")
    };

    /// <summary>What the site's program composes besides its catalogue: the web and its one model provider.</summary>
    private static readonly HostServices Services = new(WebAccess.Create, Providers: [AnthropicProvider.Instance]);

    /// <summary>
    /// Every fact of every turn of the guide's eval is met in the replay, and every conversation of the eval is
    /// replayed, over no docs at all: the baseline's first repeat of each is kept, so a conversation with no recording
    /// fails here (F126.5.1), and so does one whose tools' answers weren't recorded (FX235).
    /// </summary>
    [Test]
    public async Task TheGuidesEval_ReplayedFromItsRecordings_MeetsEveryFact_InEveryConversation_OverNoDocs()
    {
        var replayed = await EvalReplay.AssertFactsMetAsync(SiteCatalog.Create, Services, SiteSettings, "guide",
            Recordings);

        using var file = new StreamReader(new GuideIdentity().Prompts
            .GetFileInfo("agents/louis-agent-guide/guide.eval.json").CreateReadStream());
        var every = EvalFileReader.Read(file.ReadToEnd()).Conversations.Select(conversation => conversation.Id);
        Assert.That(replayed, Is.EqualTo(every));
    }
}
