using LouisAgent.Choices;
using LouisAgent.Core.Tools.Web.Rendering;
using LouisAgent.Core.Tools.Web.Search;
using LouisAgent.Testing;
using LouisAgent.Api.Benchmarks.Runs;

namespace LouisAgent.Api.Tests.Benchmarks.Runs;

/// <summary>A run keeps what it ran with, so a later run can be compared with it knowing what changed.</summary>
public class BenchmarkSettingsTests
{
    /// <summary>The runner path these tests use.</summary>
    private const string Runner = "/app/evals/louis-agent.evals.runner.dll";

    /// <summary>A run keeps its model, roles and eval, and the search engine and page renderer with whether each was set up.</summary>
    [Test]
    public void ARun_KeepsTheModelTheRolesAndTheEvalWithItsRunner()
    {
        var check = new BenchmarkCheck(SampleEval.Beta, "Cook");

        var settings = BenchmarkSettings.Of(check, "anthropic", "claude-haiku-5-5", ["puls/voice-af", SampleRole.Alpha], Runner,
            WebSearchProviderKind.Searxng, PageRendererKind.Chrome, true, true);

        Assert.That(settings, Is.EquivalentTo(new Dictionary<string, string>
        {
            ["provider"] = "anthropic",
            ["model"] = "claude-haiku-5-5",
            ["roles_enabled"] = SampleRole.Fill("puls/voice-af,<RoleAlpha>"),
            ["eval"] = SampleEval.Beta,
            ["eval_runner"] = Runner,
            ["web_search_provider"] = "searxng",
            ["web_search_provider_ready"] = "true",
            ["page_renderer"] = "chrome",
            ["page_renderer_ready"] = "true",
        }));
    }

    /// <summary>The engine and renderer are written with their wire names, not `ToString()`.</summary>
    [Test]
    public void TheSearchProviderAndTheRenderer_AreWrittenWithWire_NotToString()
    {
        // DuckDuckGo's wire text is its name in lower case, which ToString() ("DuckDuckGo") would not give.
        var settings = BenchmarkSettings.Of(new BenchmarkCheck("c", "C"), "anthropic", "m", [], Runner, WebSearchProviderKind.DuckDuckGo, PageRendererKind.Tavily, true, true);

        Assert.That((settings["web_search_provider"], settings["page_renderer"]),
            Is.EqualTo((WebSearchProviderKind.DuckDuckGo.Wire(), PageRendererKind.Tavily.Wire())));
    }

    /// <summary>A tool that was named but not set up is recorded as not ready beside its name, and a ready one as ready.</summary>
    [TestCase(true, true, "true", "true")]
    [TestCase(false, false, "false", "false")]
    [TestCase(true, false, "true", "false")]
    public void EachToolIsRecordedAsReadyOrNotReady_BesideItsName(bool searchReady, bool rendererReady, string searchText, string rendererText)
    {
        var settings = BenchmarkSettings.Of(new BenchmarkCheck("c", "C"), "anthropic", "m", [], Runner,
            WebSearchProviderKind.Tavily, PageRendererKind.Chrome, searchReady, rendererReady);

        Assert.That((settings["web_search_provider"], settings["web_search_provider_ready"], settings["page_renderer"], settings["page_renderer_ready"]),
            Is.EqualTo(("tavily", searchText, "chrome", rendererText)));
    }

    /// <summary>If there is no renderer its keys are left out, and the search engine still shows.</summary>
    [Test]
    public void ARendererThatIsNone_LeavesItsKeyOut_AndTheSearchProviderStillShows()
    {
        var settings = BenchmarkSettings.Of(new BenchmarkCheck("c", "C"), "anthropic", "m", [], Runner, WebSearchProviderKind.DuckDuckGo, null, true, false);

        Assert.That(settings.ContainsKey("page_renderer"), Is.False, "absent, never empty");
        Assert.That(settings.ContainsKey("page_renderer_ready"), Is.False, "no renderer, so nothing to be ready");
        Assert.That(settings["web_search_provider"], Is.EqualTo(WebSearchProviderKind.DuckDuckGo.Wire()));
    }

    /// <summary>The settings of a run that has just started, to add to.</summary>
    private static readonly IReadOnlyDictionary<string, string> Started = new Dictionary<string, string> { ["provider"] = "anthropic", ["model"] = "m" };

    /// <summary>The hashes are written joined with commas, in the order given.</summary>
    [Test]
    public void WithPromptHashes_AddsThemJoinedWithCommas_InTheOrderGiven()
    {
        var settings = BenchmarkSettings.WithPromptHashes(Started, ["a", "b"]);

        Assert.That(settings, Is.EquivalentTo(new Dictionary<string, string> { ["provider"] = "anthropic", ["model"] = "m", ["prompt_hashes"] = "a,b" }));
    }

    /// <summary>With no hashes the settings come back as they were, with no key at all.</summary>
    [Test]
    public void WithPromptHashes_WithNone_ReturnsTheSettingsUnchanged_WithNoKey()
    {
        var settings = BenchmarkSettings.WithPromptHashes(Started, []);

        Assert.That(settings, Is.SameAs(Started));
        Assert.That(settings!.ContainsKey("prompt_hashes"), Is.False, "absent, never empty");

        // And with no settings at all, there is still nothing to write.
        Assert.That(BenchmarkSettings.WithPromptHashes(null, []), Is.Null);
    }

    /// <summary>With no settings yet, the hashes are the only thing written.</summary>
    [Test]
    public void WithPromptHashes_OnNullSettings_ReturnsOnlyThatKey() =>
        Assert.That(BenchmarkSettings.WithPromptHashes(null, ["a"]), Is.EquivalentTo(new Dictionary<string, string> { ["prompt_hashes"] = "a" }));

    /// <summary>An empty role list turns every role on.</summary>
    [Test]
    public void EveryRole_IsOn_WhenNoneAreListed() =>
        Assert.That(BenchmarkSettings.Of(new BenchmarkCheck("c", "C"), "anthropic", "m", [], Runner, null, null, false, false)["roles_enabled"], Is.EqualTo("(all)"));

    /// <summary>An eval check records its eval and the runner it started from, and no script path any more.</summary>
    [Test]
    public void AnEvalCheckRecordsItsEvalAndItsRunner()
    {
        var settings = BenchmarkSettings.Of(new BenchmarkCheck("stub", "Stub"), "anthropic", "m", [], "/app/evals/x.dll", null, null, false, false);

        Assert.That((settings["eval"], settings["eval_runner"]), Is.EqualTo(("stub", "/app/evals/x.dll")));
        Assert.That(settings.ContainsKey("script"), Is.False, "no script path any more (F18 step 7.4)");
    }
}
