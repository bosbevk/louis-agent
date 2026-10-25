using LouisAgent.Core.Hosting.SessionLlms;
using LouisAgent.Core.Sessions.SubSessions;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Sinks;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Agent.Catalog;
using System.Net;
using System.Text;
using System.Text.Json;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Tools.Agents;
using LouisAgent.Core.Tools.Web.Search;

namespace LouisAgent.Core.Tests.Tools.Agents;

// F30 step 5: a specialist hands one task and one of its own tools to a minion on a cheap model (MinionS: small
// single-step tasks for the local model, the cloud model coordinates). The peer is a stub: no network, no model.
public class MinionToolsTests
{
    private const string Role = "puls-minion-web-price-lookup";
    private static readonly Dictionary<string, Uri> Self = new() { ["puls"] = new Uri("http://puls-agent.test/") };
    private static readonly Dictionary<string, IReadOnlyList<string>> Cascade = new() { ["*"] = ["ollama-puls", "instance"] };

    private const string Stream = """
        event: content_block_delta
        data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"shop: Checkers\nprice: R35.99"}}

        event: message_delta
        data: {"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{"input_tokens":1500,"cache_creation_input_tokens":0,"cache_read_input_tokens":0,"output_tokens":40,"requests":2,"cost":0,"currency":"USD"}}

        """;

    private static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    /// <summary>The specialist's own instance: creates sess_minion, answers with the stream and accepts the delete; keeps every create body.</summary>
    private static StubHandler Peer(List<JsonDocument> creates, Func<HttpResponseMessage>? answer = null) => new(request =>
    {
        if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath == "/v1/sessions")
        {
            creates.Add(JsonDocument.Parse(request.Content!.ReadAsStringAsync().Result));
            return Json(HttpStatusCode.Created, """{"session_id":"sess_minion","role":"puls-minion-web-price-lookup"}""");
        }
        return request.Method.Method switch
        {
            "POST" => answer?.Invoke() ?? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Stream, Encoding.UTF8, "text/event-stream") },
            "DELETE" => Json(HttpStatusCode.OK, """{"deleted":true}"""),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        };
    });

    private static MinionTools Tools(StubHandler handler, IReadOnlyDictionary<string, IReadOnlyList<string>>? llms = null, TimeSpan? timeout = null,
        IUsageSink? sink = null) => new(Self, llms ?? Cascade, "the-key", new HttpClient(handler), timeout, usageSink: sink);

    /// <summary>The cook's turn: its tools as the host sets them, under Puls's turn as the parent.</summary>
    private static IDisposable CooksTurn(string session = "sess_cook", int turn = 2, string parent = "sess_puls#1", params string[] tools)
    {
        var scope = UsageScope.Begin(host: "api", session: session, turn: turn, tags: new UsageTags(null, null, null, parent));
        var mine = AgentTools.Use(tools.Length == 0 ? ["FetchUrl", "ReadRenderedPage", "WebSearch", "ExecuteSkill"] : tools);
        return new Both(scope, mine);
    }

    private sealed class Both(IDisposable first, IDisposable second) : IDisposable
    {
        public void Dispose() { second.Dispose(); first.Dispose(); }
    }

    [Test]
    public async Task AMinion_OpensAtDepthTwo_WithItsRoleItsToolAndTheRolesModel_AndRollsUpUnderTheSpecialist()
    {
        var creates = new List<JsonDocument>();
        var sink = new ListUsageSink();
        string answer;
        using (CooksTurn())
            answer = await Tools(Peer(creates), sink: sink).StartMinion(Role, "FetchUrl", "https://shop.test/mince; beef mince 500 g; Stellenbosch");

        var body = creates.Single().RootElement;
        Assert.That(body.GetProperty("depth").GetInt32(), Is.EqualTo(2));
        Assert.That(body.GetProperty("role").GetString(), Is.EqualTo(Role), "CL14: the body says role");
        Assert.That(body.GetProperty("tool").GetString(), Is.EqualTo("FetchUrl"));
        Assert.That(body.GetProperty("llm").GetString(), Is.EqualTo("ollama-puls"), "the cheapest model in the role's list first");
        Assert.That(body.GetProperty("parent").GetString(), Is.EqualTo("sess_cook#2"), "the specialist's turn, so the ledger chains Puls → cook → minion");
        Assert.That(body.GetProperty("task").GetString(), Is.EqualTo($"minion:{Role}"));
        Assert.That(body.TryGetProperty("toolsets", out _), Is.False, "the peer derives the toolset from the tool");

        Assert.That(answer, Does.StartWith("shop: Checkers").And.Contain("[Minion puls-minion-web-price-lookup on ollama-puls with FetchUrl"));
        UsageRecord row = sink.Records.Single();
        Assert.That((row.Purpose, row.Model, row.Session, row.Turn, row.Parent), Is.EqualTo(("sub_session", $"minion:{Role}", "sess_cook", (int?)2, "sess_puls#1")));
        Assert.That(row.Cost, Is.EqualTo(new UsageCost("USD", 0m, null)), "the local model's cost, 0, is still a number: never unknown");
    }

    [Test]
    public async Task AToolTheSpecialistDoesntHold_OrNeverHandedDown_OrARoleThatIsntAMinions_IsRefused_WithoutANetworkCall()
    {
        var creates = new List<JsonDocument>();
        var tools = Tools(Peer(creates));

        using (CooksTurn(tools: ["FetchUrl"]))
        {
            Assert.That(await tools.StartMinion(Role, "ReadRenderedPage", "a page"), Does.StartWith("Error:").And.Contain("ReadRenderedPage").And.Contain("FetchUrl"),
                "only one of its own: the parent's toolbox is the ceiling");
            Assert.That(await tools.StartMinion(Role, "ExecuteSkill", "a page"), Does.StartWith("Error:").And.Contain("never"));
            Assert.That(await tools.StartMinion("puls-cook", "FetchUrl", "a page"), Does.StartWith("Error:").And.Contain("puls-cook"), "a specialist's role is no minion's");
            Assert.That(await tools.StartMinion("puls-minion", "FetchUrl", "a page"), Does.StartWith("Error:"), "the base is no role");
        }

        Assert.That(creates, Is.Empty);
    }

    [Test]
    public async Task Stronger_RunsTheSameTaskOnTheNextModel_AndIsRefusedAtTheEndOfTheList()
    {
        // FrugalGPT's cascade with VerifyQuote as the check: the specialist asks for the next model, never names one.
        var creates = new List<JsonDocument>();
        var tools = Tools(Peer(creates));

        using (CooksTurn())
        {
            await tools.StartMinion(Role, "FetchUrl", "a page", stronger: true);
            Assert.That(creates.Single().RootElement.TryGetProperty("llm", out var llm) && llm.ValueKind == JsonValueKind.String, Is.False,
                "'instance' is the specialist's own model: no llm in the body");

            var oneModel = Tools(Peer(creates), new Dictionary<string, IReadOnlyList<string>> { ["*"] = ["ollama-puls"] });
            string refused = await oneModel.StartMinion(Role, "FetchUrl", "a page", stronger: true);
            Assert.That(refused, Does.StartWith("Error:").And.Contain("ollama-puls"), "no stronger model left: the refusal says which list ended");
            Assert.That(creates, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public async Task ARolesOwnModelList_BeatsTheDefault_AndARoleWithNeither_IsRefused()
    {
        var creates = new List<JsonDocument>();
        var llms = new Dictionary<string, IReadOnlyList<string>> { [Role] = ["instance"] };

        using (CooksTurn())
        {
            await Tools(Peer(creates), llms).StartMinion(Role, "FetchUrl", "a page");
            Assert.That(creates.Single().RootElement.TryGetProperty("llm", out var llm) && llm.ValueKind == JsonValueKind.String, Is.False);

            string refused = await Tools(Peer(creates), llms).StartMinion("puls-minion-web-summary", "FetchUrl", "a page");
            Assert.That(refused, Does.StartWith("Error:").And.Contain("MINION_LLMS"), "no list for the role and no default");
            Assert.That(creates, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public async Task OnePastTheCap_IsRefused_AcrossSpecialists()
    {
        // One GPU answers one request at a time, so the limit counts the whole tree of Puls's turn: two specialists
        // under the same Puls turn share it. Every shop's price goes through a minion (Louis, F30 hand check), so a meal
        // of about eight ingredients at two shops needs sixteen.
        Assert.That(MinionTools.MaxPerTurn, Is.EqualTo(16));
        var creates = new List<JsonDocument>();
        var tools = Tools(Peer(creates));
        int first = MinionTools.MaxPerTurn - 2;

        using (CooksTurn(session: "sess_cook", turn: 1, parent: "sess_puls#4"))
            for (int i = 0; i < first; i++) Assert.That(await tools.StartMinion(Role, "FetchUrl", $"page {i}"), Does.Not.StartWith("Error:"));
        using (CooksTurn(session: "sess_researcher", turn: 1, parent: "sess_puls#4"))
        {
            for (int i = 0; i < 2; i++) Assert.That(await tools.StartMinion(Role, "FetchUrl", $"page {i}"), Does.Not.StartWith("Error:"));
            Assert.That(await tools.StartMinion(Role, "FetchUrl", "one more"), Does.StartWith("Error:").And.Contain("16"));
        }
        using (CooksTurn(session: "sess_cook", turn: 1, parent: "sess_puls#5"))
            Assert.That(await tools.StartMinion(Role, "FetchUrl", "next turn"), Does.Not.StartWith("Error:"), "another user turn starts fresh");

        Assert.That(creates, Has.Count.EqualTo(MinionTools.MaxPerTurn + 1));
    }

    [Test]
    public async Task AMinion_IsStoppedAfterItsOwnTimeout_AndDeleted()
    {
        var creates = new List<JsonDocument>();
        var deleted = false;
        var handler = new StubHandler(request =>
        {
            if (request.Method == HttpMethod.Delete) { deleted = true; return Json(HttpStatusCode.OK, "{}"); }
            if (request.RequestUri!.AbsolutePath == "/v1/sessions") { creates.Add(JsonDocument.Parse(request.Content!.ReadAsStringAsync().Result)); return Json(HttpStatusCode.Created, """{"session_id":"sess_minion"}"""); }
            Thread.Sleep(2000);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Stream, Encoding.UTF8, "text/event-stream") };
        });

        string answer;
        using (CooksTurn())
            answer = await Tools(handler, timeout: TimeSpan.FromMilliseconds(200)).StartMinion(Role, "FetchUrl", "a slow page");

        Assert.That(answer, Does.StartWith("Error:").And.Contain("didn't finish"));
        Assert.That(deleted, Is.True);
        Assert.That(MinionTools.DefaultTimeout, Is.EqualTo(TimeSpan.FromMinutes(2)), "inside the specialist's 5");
    }

    [Test]
    public async Task InAChat_AMinionIsNeverKept_ItIsDeletedOnceItAnswers()
    {
        // F30, the fourth hand check: a chat keeps three children, so kept minions evicted the cook's earlier ones and
        // its ContinueMinion calls failed. A minion answers once and closes (Louis: "do all three").
        var creates = new List<JsonDocument>();
        int deletes = 0;
        var handler = new StubHandler(request =>
        {
            if (request.Method == HttpMethod.Delete) { deletes++; return Json(HttpStatusCode.OK, "{}"); }
            if (request.RequestUri!.AbsolutePath == "/v1/sessions")
            {
                creates.Add(JsonDocument.Parse(request.Content!.ReadAsStringAsync().Result));
                return Json(HttpStatusCode.Created, """{"session_id":"sess_minion"}""");
            }
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Stream, Encoding.UTF8, "text/event-stream") };
        });
        var registry = new SubSessionRegistry();

        string answer;
        using (CooksTurn())
        using (SubSessionRegistry.Use(registry))
            answer = await Tools(handler).StartMinion(Role, "FetchUrl", "a page");

        Assert.That(answer, Does.StartWith("shop: Checkers").And.Not.Contain("handle"));
        Assert.That(registry.Open, Is.Empty, "the chat's three places stay for its specialists");
        Assert.That(deletes, Is.EqualTo(1));
        Assert.That(typeof(MinionTools).GetMethod("ContinueMinion"), Is.Null, "nothing to continue");
    }

    [Test]
    public void TheMinionModels_AreParsedFromMinionLlms_RoleOrStar_CheapestFirst()
    {
        var llms = AgentOptions.ParseMinionLlms(" *=Ollama-Puls > instance , puls-minion-web-summary=instance, broken, =x, puls-minion-a=");

        Assert.That(llms["*"], Is.EqualTo(new[] { "ollama-puls", "instance" }));
        Assert.That(llms["puls-minion-web-summary"], Is.EqualTo(new[] { "instance" }));
        Assert.That(llms.Keys, Is.EquivalentTo(new[] { "*", "puls-minion-web-summary" }), "an entry without a role or a model is dropped");
        Assert.That(AgentOptions.FromEnvironment(name => name == "MINION_LLMS" ? "*=ollama-puls>instance" : null).MinionLlms["*"], Is.EqualTo(new[] { "ollama-puls", "instance" }));
        Assert.That(AgentOptions.FromEnvironment(_ => null).MinionLlms, Is.Empty, "unset: no minions toolset");
    }

    [Test]
    public void AMinionModel_ThatIsntAProfileOrTheInstance_StopsStartUp()
    {
        var agent = new AgentOptions { SessionLlmProfiles = ["ollama-puls"], MinionLlms = new Dictionary<string, IReadOnlyList<string>> { ["*"] = ["ollama-puls", "gpt-local"] } };

        var ex = Assert.Throws<SessionLlmException>(() => SessionLlmLoader.CheckMinionLlms(agent))!;

        Assert.That(ex.Message, Does.Contain("gpt-local").And.Contain("ollama-puls").And.Contain("instance"));
        Assert.DoesNotThrow(() => SessionLlmLoader.CheckMinionLlms(new AgentOptions { SessionLlmProfiles = ["ollama-puls"], MinionLlms = new Dictionary<string, IReadOnlyList<string>> { ["*"] = ["instance", "ollama-puls"] } }));
    }

    [Test]
    public void TheMinionsToolset_NeedsPeersAndModels_AndATool_NamesItsToolset()
    {
        var minions = ToolsetCatalog.Find("minions")!;
        var without = new ToolsetContext(new AgentOptions { RatesUrl = null }, new NoSearch());
        var with = new ToolsetContext(new AgentOptions { RatesUrl = null, AgentPeers = Self, MinionLlms = Cascade }, new NoSearch());

        Assert.That(minions.MissingSettings(without), Is.EqualTo(new[] { "AGENT_PEERS", "MINION_LLMS" }));
        Assert.That(minions.IsAvailable(with), Is.True);
        Assert.That(minions.GuideFile, Is.EqualTo("toolsets/minions.md"));
        Assert.That(AgentCatalog.CoreOnly.ToolsetOf("FetchUrl"), Is.EqualTo("web"));
        Assert.That(AgentCatalog.CoreOnly.ToolsetOf("ReadWorkspaceFile"), Is.EqualTo("files"));
        Assert.That(AgentCatalog.CoreOnly.ToolsetOf("CreateTool"), Is.Null, "engine-provided: no toolset builds it");
    }

    private sealed class NoSearch : LouisAgent.Core.Tools.Web.Search.IWebSearchProvider
    {
        public string Name => "none";
        public Task<IReadOnlyList<LouisAgent.Core.Tools.Web.Search.WebSearchResult>> SearchAsync(LouisAgent.Core.Tools.Web.Search.WebSearchQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<LouisAgent.Core.Tools.Web.Search.WebSearchResult>>([]);
    }
}
