using LouisAgent.Choices;
using LouisAgent.Core.Tools.Web.Rendering;
using LouisAgent.Core.Tools.Web.Search;
using LouisAgent.Testing;
using LouisAgent.Api.Benchmarks.Runs;

namespace LouisAgent.Api.Tests.Benchmarks.Runs;

// F31 step 6a: a run keeps what it ran with, so a later run is compared with an earlier one knowing what changed.
public class BenchmarkSettingsTests
{
    private const string Runner = "/app/evals/louis-agent.evals.runner.dll";

    [Test]
    public void ARun_KeepsTheModelTheRolesAndTheEvalWithItsRunner()
    {
        var check = new BenchmarkCheck(SampleEval.Beta, "Cook");

        var settings = BenchmarkSettings.Of(check, "anthropic", "claude-haiku-5-5", ["puls/voice-af", SampleRole.Alpha], Runner,
            WebSearchProviderKind.Searxng, PageRendererKind.Chrome);

        Assert.That(settings, Is.EquivalentTo(new Dictionary<string, string>
        {
            ["provider"] = "anthropic",
            ["model"] = "claude-haiku-5-5",
            ["roles_enabled"] = SampleRole.Fill("puls/voice-af,<RoleAlpha>"),
            ["eval"] = SampleEval.Beta,
            ["eval_runner"] = Runner,
            ["web_search_provider"] = "searxng",
            ["page_renderer"] = "chrome",
        }));
    }

    [Test]
    public void TheSearchProviderAndTheRenderer_AreWrittenWithWire_NotToString()
    {
        // DuckDuckGo's wire text is its name in lower case, which ToString() ("DuckDuckGo") would not give.
        var settings = BenchmarkSettings.Of(new BenchmarkCheck("c", "C"), "anthropic", "m", [], Runner, WebSearchProviderKind.DuckDuckGo, PageRendererKind.Tavily);

        Assert.That((settings["web_search_provider"], settings["page_renderer"]),
            Is.EqualTo((WebSearchProviderKind.DuckDuckGo.Wire(), PageRendererKind.Tavily.Wire())));
    }

    [Test]
    public void ARendererThatIsNone_LeavesItsKeyOut_AndTheSearchProviderStillShows()
    {
        var settings = BenchmarkSettings.Of(new BenchmarkCheck("c", "C"), "anthropic", "m", [], Runner, WebSearchProviderKind.DuckDuckGo, null);

        Assert.That(settings.ContainsKey("page_renderer"), Is.False, "absent, never empty");
        Assert.That(settings["web_search_provider"], Is.EqualTo(WebSearchProviderKind.DuckDuckGo.Wire()));
    }

    private static readonly IReadOnlyDictionary<string, string> Started = new Dictionary<string, string> { ["provider"] = "anthropic", ["model"] = "m" };

    [Test]
    public void WithPromptHashes_AddsThemJoinedWithCommas_InTheOrderGiven()
    {
        var settings = BenchmarkSettings.WithPromptHashes(Started, ["a", "b"]);

        Assert.That(settings, Is.EquivalentTo(new Dictionary<string, string> { ["provider"] = "anthropic", ["model"] = "m", ["prompt_hashes"] = "a,b" }));
    }

    [Test]
    public void WithPromptHashes_WithNone_ReturnsTheSettingsUnchanged_WithNoKey()
    {
        var settings = BenchmarkSettings.WithPromptHashes(Started, []);

        Assert.That(settings, Is.SameAs(Started));
        Assert.That(settings!.ContainsKey("prompt_hashes"), Is.False, "absent, never empty");
        Assert.That(BenchmarkSettings.WithPromptHashes(null, []), Is.Null);
    }

    [Test]
    public void WithPromptHashes_OnNullSettings_ReturnsOnlyThatKey() =>
        Assert.That(BenchmarkSettings.WithPromptHashes(null, ["a"]), Is.EquivalentTo(new Dictionary<string, string> { ["prompt_hashes"] = "a" }));

    [Test]
    public void EveryRole_IsOn_WhenNoneAreListed() =>
        Assert.That(BenchmarkSettings.Of(new BenchmarkCheck("c", "C"), "anthropic", "m", [], Runner, null, null)["roles_enabled"], Is.EqualTo("(all)"));

    [Test]
    public void AnEvalCheckRecordsItsEvalAndItsRunner()
    {
        var settings = BenchmarkSettings.Of(new BenchmarkCheck("stub", "Stub"), "anthropic", "m", [], "/app/evals/x.dll", null, null);

        Assert.That((settings["eval"], settings["eval_runner"]), Is.EqualTo(("stub", "/app/evals/x.dll")));
        Assert.That(settings.ContainsKey("script"), Is.False, "no script path any more (F18 step 7.4)");
    }
}
