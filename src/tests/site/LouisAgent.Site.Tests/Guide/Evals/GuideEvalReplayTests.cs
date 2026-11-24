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
    private static readonly Lazy<string> ImageDocs = new(MakeImageDocs);

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
        ["DOCS_ROOT"] = ImageDocs.Value,
        ["PRICES_FILE"] = Path.Combine(TestPaths.RepoRoot, "src", "site", "config", "prices.json"),
        ["RATES_FILE"] = Path.Combine(TestPaths.RepoRoot, "src", "site", "config", "rates.json")
    };

    /// <summary>What the site's program composes besides its catalogue: the web and its one model provider.</summary>
    private static readonly HostServices Services = new(WebAccess.Create, Providers: [AnthropicProvider.Instance]);

    /// <summary>
    /// Every fact of every turn of the guide's eval is met in the replay, and every conversation of the eval is
    /// replayed: the baseline's first repeat of each is kept, so a conversation with no recording fails here
    /// (F126.5.1).
    /// </summary>
    [Test]
    public async Task TheGuidesEval_ReplayedFromItsRecordings_MeetsEveryFact_InEveryConversation()
    {
        var replayed = await EvalReplay.AssertFactsMetAsync(SiteCatalog.Create, Services, SiteSettings, "guide",
            Recordings);

        using var file = new StreamReader(new GuideIdentity().Prompts
            .GetFileInfo("agents/louis-agent-guide/guide.eval.json").CreateReadStream());
        var every = EvalFileReader.Read(file.ReadToEnd()).Conversations.Select(conversation => conversation.Id);
        Assert.That(replayed, Is.EqualTo(every));
    }

    /// <summary>
    /// The docs as the site's image holds them, at <c>/app/docs</c>: the root's README, ROADMAP and TODO, the
    /// framework's docs without the films, the demo's docs and the packs' docs (<c>src/site/docker/Dockerfile</c>). The
    /// guide's search reads whatever folder it is given, so the replay gives it the folder the recording was made over
    /// (F126.5.1: over the whole repository a search found other lines, and the replay stopped). Made once, in a temp
    /// folder.
    /// </summary>
    private static string MakeImageDocs()
    {
        string root = TestPaths.RepoRoot;
        string docs = Directory.CreateTempSubdirectory("guide-image-docs-").FullName;
        foreach (string file in new[] { "README.md", "ROADMAP.md", "TODO.md" })
            File.Copy(Path.Combine(root, file), Path.Combine(docs, file));
        CopyFolder(Path.Combine(root, "src", "docs"), Path.Combine(docs, "src", "docs"));
        CopyFolder(Path.Combine(root, "demos", "louis-agent", "docs"),
            Path.Combine(docs, "demos", "louis-agent", "docs"));
        CopyFolder(Path.Combine(root, "packs", "docs"), Path.Combine(docs, "packs", "docs"));
        Directory.Delete(Path.Combine(docs, "src", "docs", "films"), recursive: true);
        return docs;
    }

    /// <summary>A folder copied whole, as the image's COPY does.</summary>
    private static void CopyFolder(string from, string to)
    {
        foreach (string file in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(to, Path.GetRelativePath(from, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }
}
