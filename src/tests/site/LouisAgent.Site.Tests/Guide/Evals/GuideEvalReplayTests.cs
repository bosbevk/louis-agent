using LouisAgent.Api.Evals.Testing;
using LouisAgent.Core.Anthropic;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Web;
using LouisAgent.SiteApi;

namespace LouisAgent.Site.Tests.Guide.Evals;

/// <summary>
/// The guide's eval, replayed from the recordings kept in git: every fact of every turn is met, with no key and no
/// model. A change to the guide's prompt, its tools or a doc it reads asks the recording something it doesn't hold,
/// and this turns red until a live run records again.
/// </summary>
public class GuideEvalReplayTests
{
    /// <summary>Where the guide's eval recordings are kept, beside the pages' recordings.</summary>
    private static readonly string Recordings = Path.Combine(TestPaths.RepoRoot, "src", "site", "recordings", "evals");

    /// <summary>
    /// The settings the site's container runs the guide with (its compose file and its tracked model settings), with
    /// the docs where the repository keeps them and a key that is never used, since nothing calls a model.
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
        ["DOCS_ROOT"] = TestPaths.RepoRoot,
        ["PRICES_FILE"] = Path.Combine(TestPaths.RepoRoot, "src", "site", "config", "prices.json"),
        ["RATES_FILE"] = Path.Combine(TestPaths.RepoRoot, "src", "site", "config", "rates.json")
    };

    /// <summary>What the site's program composes besides its catalogue: the web and its one model provider.</summary>
    private static readonly HostServices Services = new(WebAccess.Create, Providers: [AnthropicProvider.Instance]);

    /// <summary>Every fact of every turn of the guide's eval is met in the replay.</summary>
    [Test]
    public Task TheGuidesEval_ReplayedFromItsRecordings_MeetsEveryFact() =>
        EvalReplay.AssertFactsMetAsync(SiteCatalog.Create, Services, SiteSettings, "guide", Recordings);
}
