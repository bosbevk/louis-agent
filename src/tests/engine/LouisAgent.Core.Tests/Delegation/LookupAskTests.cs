using LouisAgent.Core.Web;
using LouisAgent.Composition.Web.Search;
using LouisAgent.Choices;
using LouisAgent.Core.Hosting.SessionLlms;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Sessions.SubSessions;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Sinks;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Agent.Catalog;
using System.Net;
using System.Text;
using System.Text.Json;
using LouisAgent.Core.Config;
using LouisAgent.Core.Delegation;
using LouisAgent.Core.Delegation.Agents;
using LouisAgent.Composition.Sessions;

namespace LouisAgent.Core.Tests.Delegation;

/// <summary>
/// A subagent hands one task and one of its own tools to a lookup agent on a cheap model (F30 step 5: small
/// single-step tasks for the local model, while the cloud model coordinates). The peer is a stub: no network, no model.
/// </summary>
public class LookupAskTests
{
    /// <summary>The lookup agent every test hands work to, by its address (F127.5).</summary>
    private const string Address = "testprod.lookup";

    /// <summary>That lookup agent's role file, which the session opens with.</summary>
    private const string Role = "testprod.lookup";

    /// <summary>The identity the cook's turn runs as: it may ask that lookup agent and its own voice, nothing
    /// else.</summary>
    private static readonly FakeIdentity Testprod =
        new("testprod", voice: "testprod/voice-en", calls: [Address, "testprod"]);

    /// <summary>This instance as its own peer, where a lookup agent's session opens.</summary>
    private static readonly Dictionary<string, Uri> Self = new()
    {
        [SampleIdentity.Alpha] = new Uri("http://puls-agent.test/")
    };

    /// <summary>Every role's models, cheapest first: the local model, then the instance's own.</summary>
    private static readonly Dictionary<string, IReadOnlyList<string>> Cascade = new()
    {
        ["*"] = ["ollama-puls", "instance"]
    };

    /// <summary>A lookup agent's answer: one shop and its price, and what it used on the local model.</summary>
    private const string Stream = """
                                  event: content_block_delta
                                  data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"shop: Checkers\nprice: R35.99"}}

                                  event: message_delta
                                  data: {"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{"input_tokens":1500,"cache_creation_input_tokens":0,"cache_read_input_tokens":0,"output_tokens":40,"requests":2,"cost":0,"currency":"USD"}}

                                  """;

    /// <summary>A JSON reply with the status given.</summary>
    private static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    /// <summary>
    /// The subagent's own instance: it opens sess_lookup, answers with the stream and accepts the delete, and keeps
    /// every body it was asked to open a session with.
    /// </summary>
    private static StubHandler Peer(List<JsonDocument> creates, Func<HttpResponseMessage>? answer = null) =>
        new(request =>
        {
            if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath == "/v1/sessions")
            {
                creates.Add(JsonDocument.Parse(request.Content!.ReadAsStringAsync().Result));
                return Json(HttpStatusCode.Created,
                    $$"""{"session_id":"sess_lookup","role":"{{Role}}"}""");
            }

            return request.Method.Method switch
            {
                "POST" => answer?.Invoke() ?? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(Stream, Encoding.UTF8, "text/event-stream")
                },
                "DELETE" => Json(HttpStatusCode.OK, """{"deleted":true}"""),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound),
            };
        });

    /// <summary>
    /// <c>AskAgent</c> on the stub peer, as a subagent asks its lookup agent (F127.5: "it should use askagent also"),
    /// with the default models unless a test gives its own.
    /// </summary>
    private static Lookups Tools(StubHandler handler,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? llms = null, TimeSpan? timeout = null,
        IUsageSink? sink = null) =>
        new(new AskAgentTools(Self, [Testprod], "the-key", new HttpClient(handler), usageSink: sink,
            lookupLlms: llms ?? Cascade, lookupTimeout: timeout));

    /// <summary>A lookup agent asked through <c>AskAgent</c>, its arguments in the order these tests write
    /// them.</summary>
    private sealed class Lookups(AskAgentTools tools)
    {
        /// <summary>Asks the lookup agent at the address with one of the caller's tools.</summary>
        public Task<string> AskLookupAgent(string address, string tool, string task, bool stronger = false) =>
            tools.AskAgent(address, task, tool: tool, stronger: stronger);
    }

    /// <summary>
    /// The cook's turn: its tools as the host sets them, run as its identity one level down, under Puls's turn as the
    /// parent.
    /// </summary>
    private static IDisposable CooksTurn(string session = "sess_cook", int turn = 2, string parent = "sess_puls#1",
        params string[] tools)
    {
        var scope = UsageScope.Begin(host: "api", session: session, turn: turn,
            tags: new UsageTags(null, null, null, parent));
        var mine = AgentTools.Use(tools.Length == 0
            ? ["FetchUrl", "ReadRenderedPage", "WebSearch", "ExecuteSkill"]
            : tools);
        return new All([scope, mine, TurnIdentity.Use(TurnIdentity.Of(Testprod)), TurnDepth.Use(1)]);
    }

    /// <summary>Scopes that end together, the last begun first.</summary>
    private sealed class All(IReadOnlyList<IDisposable> scopes) : IDisposable
    {
        /// <summary>Ends every scope, in the reverse of the order they began.</summary>
        public void Dispose()
        {
            foreach (var scope in scopes.Reverse()) scope.Dispose();
        }
    }

    /// <summary>
    /// A lookup agent opens two levels down with its role, its one tool and the role's model, and what it used is
    /// rolled up under the subagent's turn.
    /// </summary>
    [Test]
    public async Task ALookupAgent_OpensAtDepthTwo_WithItsRoleItsToolAndTheRolesModel_AndRollsUpUnderTheSubagent()
    {
        var creates = new List<JsonDocument>();
        var sink = new ListUsageSink();
        string answer;
        using (CooksTurn())
            answer = await Tools(Peer(creates), sink: sink).AskLookupAgent(Address, "FetchUrl",
                "https://shop.test/mince; beef mince 500 g; Stellenbosch");

        var body = creates.Single().RootElement;
        Assert.That(body.GetProperty("depth").GetInt32(), Is.EqualTo(2));
        Assert.That(body.GetProperty("role").GetString(), Is.EqualTo(Role), "CL14: the body says role");
        Assert.That(body.GetProperty("tool").GetString(), Is.EqualTo("FetchUrl"));
        Assert.That(body.GetProperty("llm").GetString(), Is.EqualTo("ollama-puls"),
            "the cheapest model in the role's list first");
        Assert.That(body.GetProperty("parent").GetString(), Is.EqualTo("sess_cook#2"),
            "the subagent's turn, so the ledger chains Puls → cook → lookup agent");
        Assert.That(body.GetProperty("task").GetString(), Is.EqualTo($"lookup:{Role}"));
        Assert.That(body.GetProperty("toolsets").ValueKind, Is.EqualTo(JsonValueKind.Null),
            "the peer derives the toolset from the tool");

        Assert.That(answer,
            Does.StartWith("shop: Checkers").And
                .Contain($"[Lookup {Role} on ollama-puls with FetchUrl"));
        UsageRecord row = sink.Records.Single();
        Assert.That((row.Purpose, row.Model, row.Session, row.Turn, row.Parent),
            Is.EqualTo(("sub_session", $"lookup:{Role}", "sess_cook", (int?)2, "sess_puls#1")));
        Assert.That(row.Cost, Is.EqualTo(new UsageCost("USD", 0m, null)),
            "the local model's cost, 0, is still a number: never unknown");
    }

    /// <summary>
    /// A lookup agent's answer reads the same whatever it used: on a replay its usage is zero, and the subagent's next
    /// request must still match its recording (FX27).
    /// </summary>
    [Test]
    public async Task TheAnswer_ReadsTheSame_WhateverTheLookupAgentUsed()
    {
        string replayedStream = Stream.Replace("\"input_tokens\":1500", "\"input_tokens\":0")
            .Replace("\"output_tokens\":40", "\"output_tokens\":0");

        string recorded = await AnswerFrom(Stream);
        string replayed = await AnswerFrom(replayedStream);

        Assert.That(replayed, Is.EqualTo(recorded));
    }

    /// <summary>What the subagent reads back from one lookup agent whose stream is <paramref name="stream"/>.</summary>
    private static async Task<string> AnswerFrom(string stream)
    {
        var answer = () => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(stream, Encoding.UTF8, "text/event-stream")
        };
        using (CooksTurn())
            return await Tools(Peer([], answer)).AskLookupAgent(Address, "FetchUrl", "https://shop.test/mince");
    }

    /// <summary>A lookup agent records into, or replays from, its chat's own trial; with no trial it gets
    /// none.</summary>
    [Test]
    public async Task TheReplay_GoesToTheLookupAgent_AsTheParentsOwnTrial_AndNoneWhenThereIsNone()
    {
        // F18 step 4a: a lookup agent that reads a page records into, or replays from, the trial of the chat it works
        // for.
        var withReplay = new List<JsonDocument>();
        using (SessionReplay.Use(new SessionReplay(WebReplayMode.Replay,
                   SampleMode.Fill("run_1:<ModeBeta>:1/bobotie"), Model: false)))
        using (CooksTurn())
            await Tools(Peer(withReplay))
                .AskLookupAgent(Address, "FetchUrl", "https://shop.test/mince; beef mince 500 g; Stellenbosch");
        var without = new List<JsonDocument>();
        using (CooksTurn())
            await Tools(Peer(without))
                .AskLookupAgent(Address, "FetchUrl", "https://shop.test/mince; beef mince 500 g; Stellenbosch");

        var replay = withReplay.Single().RootElement.GetProperty("replay");
        Assert.That(
            (replay.GetProperty("web").GetString(), replay.GetProperty("of").GetString(),
                replay.GetProperty("model").GetBoolean()),
            Is.EqualTo(("replay", SampleMode.Fill("run_1:<ModeBeta>:1/bobotie"), false)));
        Assert.That(
            without.Single().RootElement.TryGetProperty("replay", out var none) &&
            none.ValueKind == JsonValueKind.Object, Is.False);
    }

    /// <summary>
    /// A tool the subagent doesn't hold, one never handed down, or a role that isn't a lookup agent's is refused before
    /// anything is sent.
    /// </summary>
    [Test]
    public async Task
        AToolTheSubagentDoesntHold_OrNeverHandedDown_OrARoleThatIsntALookupAgents_IsRefused_WithoutANetworkCall()
    {
        var creates = new List<JsonDocument>();
        var tools = Tools(Peer(creates));

        using (CooksTurn(tools: ["FetchUrl"]))
        {
            Assert.That(await tools.AskLookupAgent(Address, "ReadRenderedPage", "a page"),
                Does.StartWith("Error:").And.Contain("ReadRenderedPage").And.Contain("FetchUrl"),
                "only one of its own: the parent's toolbox is the ceiling");
            Assert.That(await tools.AskLookupAgent(Address, "ExecuteSkill", "a page"),
                Does.StartWith("Error:").And.Contain("never"));
            Assert.That(await tools.AskLookupAgent("testprod.researcher", "FetchUrl", "a page"),
                Does.StartWith("Error:").And.Contain("testprod.researcher"), "a subagent is no lookup agent");
            Assert.That(await tools.AskLookupAgent("testprod", "FetchUrl", "a page"),
                Does.StartWith("Error:").And.Contain("only a lookup agent"), "an identity alone is no lookup agent");
            Assert.That(await tools.AskLookupAgent("not an address", "FetchUrl", "a page"), Does.StartWith("Error:"));
        }

        Assert.That(creates, Is.Empty);
    }

    /// <summary>A lookup agent's address without a tool is refused before anything is sent: a lookup agent runs one
    /// tool.</summary>
    [Test]
    public async Task ALookupAgentAskedWithoutATool_IsRefused_WithoutANetworkCall()
    {
        var creates = new List<JsonDocument>();
        var tools = new AskAgentTools(Self, [Testprod], "the-key", new HttpClient(Peer(creates)),
            lookupLlms: Cascade);

        using (CooksTurn())
            Assert.That(await tools.AskAgent(Address, "a page"),
                Does.StartWith("Error:").And.Contain(Address).And.Contain("tool"));

        Assert.That(creates, Is.Empty);
    }

    /// <summary>
    /// A lookup agent the identity's <c>Calls</c> don't list is refused naming the ones they do, and a lookup agent on
    /// another server is refused: a lookup agent runs here, on a local model (F127.5, ADR 0081).
    /// </summary>
    [Test]
    public async Task ALookupAgentNotListed_OrOnAnotherServer_IsRefused_WithoutANetworkCall()
    {
        var creates = new List<JsonDocument>();
        var tools = Tools(Peer(creates));

        using (CooksTurn())
        {
            Assert.That(await tools.AskLookupAgent("testprod.summary", "FetchUrl", "a page"),
                Does.StartWith("Error:").And.Contain("testprod.summary").And.Contain(Address));
            Assert.That(await tools.AskLookupAgent("partner:testprod.lookup", "FetchUrl", "a page"),
                Does.StartWith("Error:").And.Contain("this instance"));
        }

        Assert.That(creates, Is.Empty);
    }

    /// <summary>
    /// Asking for a stronger model runs the task on the next one in the list, and is refused past its end.
    /// </summary>
    [Test]
    public async Task Stronger_RunsTheSameTaskOnTheNextModel_AndIsRefusedAtTheEndOfTheList()
    {
        // FrugalGPT's cascade with VerifyQuote as the check: the subagent asks for the next model, never names one.
        var creates = new List<JsonDocument>();
        var tools = Tools(Peer(creates));

        using (CooksTurn())
        {
            await tools.AskLookupAgent(Address, "FetchUrl", "a page", stronger: true);
            Assert.That(
                creates.Single().RootElement.TryGetProperty("llm", out var llm) &&
                llm.ValueKind == JsonValueKind.String, Is.False,
                "'instance' is the subagent's own model: no llm in the body");

            var oneModel = Tools(Peer(creates),
                new Dictionary<string, IReadOnlyList<string>> { ["*"] = ["ollama-puls"] });
            string refused = await oneModel.AskLookupAgent(Address, "FetchUrl", "a page", stronger: true);
            Assert.That(refused, Does.StartWith("Error:").And.Contain("ollama-puls"),
                "no stronger model left: the refusal says which list ended");
            Assert.That(creates, Has.Count.EqualTo(1));
        }
    }

    /// <summary>A role's own list of models wins over the default, and a role with neither is refused.</summary>
    [Test]
    public async Task ARolesOwnModelList_BeatsTheDefault_AndARoleWithNeither_IsRefused()
    {
        var creates = new List<JsonDocument>();
        var llms = new Dictionary<string, IReadOnlyList<string>> { [Role] = ["instance"] };

        using (CooksTurn())
        {
            await Tools(Peer(creates), llms).AskLookupAgent(Address, "FetchUrl", "a page");
            Assert.That(
                creates.Single().RootElement.TryGetProperty("llm", out var llm) &&
                llm.ValueKind == JsonValueKind.String, Is.False);

            var noListForIt = new Dictionary<string, IReadOnlyList<string>> { ["another-lookup"] = ["instance"] };
            string refused = await Tools(Peer(creates), noListForIt).AskLookupAgent(Address, "FetchUrl", "a page");
            Assert.That(refused, Does.StartWith("Error:").And.Contain("LOOKUP_LLMS"),
                "no list for the role and no default");
            Assert.That(creates, Has.Count.EqualTo(1));
        }
    }

    /// <summary>
    /// One lookup agent past the cap is refused, counted across every subagent of the same user turn; the next turn
    /// starts fresh.
    /// </summary>
    [Test]
    public async Task OnePastTheCap_IsRefused_AcrossSubagents()
    {
        // One GPU answers one request at a time, so the limit counts the whole tree of Puls's turn: two subagents
        // under the same Puls turn share it. Every shop's price goes through a lookup agent (Louis, F30 hand check), so
        // a meal of about eight ingredients at two shops needs sixteen.
        Assert.That(LookupAsk.MaxPerTurn, Is.EqualTo(16));
        var creates = new List<JsonDocument>();
        var tools = Tools(Peer(creates));
        int first = LookupAsk.MaxPerTurn - 2;

        using (CooksTurn(session: "sess_cook", turn: 1, parent: "sess_puls#4"))
            for (int i = 0; i < first; i++)
                Assert.That(await tools.AskLookupAgent(Address, "FetchUrl", $"page {i}"), Does.Not.StartWith("Error:"));
        using (CooksTurn(session: "sess_researcher", turn: 1, parent: "sess_puls#4"))
        {
            for (int i = 0; i < 2; i++)
                Assert.That(await tools.AskLookupAgent(Address, "FetchUrl", $"page {i}"), Does.Not.StartWith("Error:"));
            Assert.That(await tools.AskLookupAgent(Address, "FetchUrl", "one more"),
                Does.StartWith("Error:").And.Contain("16"));
        }

        using (CooksTurn(session: "sess_cook", turn: 1, parent: "sess_puls#5"))
            Assert.That(await tools.AskLookupAgent(Address, "FetchUrl", "next turn"), Does.Not.StartWith("Error:"),
                "another user turn starts fresh");

        Assert.That(creates, Has.Count.EqualTo(LookupAsk.MaxPerTurn + 1));
    }

    /// <summary>A lookup agent that takes too long is stopped after its own timeout and deleted on the peer.</summary>
    [Test]
    public async Task ALookupAgent_IsStoppedAfterItsOwnTimeout_AndDeleted()
    {
        var deleted = false;
        var handler = new StubHandler(request =>
        {
            if (request.Method == HttpMethod.Delete)
            {
                deleted = true;
                return Json(HttpStatusCode.OK, "{}");
            }

            if (request.RequestUri!.AbsolutePath == "/v1/sessions")
                return Json(HttpStatusCode.Created, """{"session_id":"sess_lookup"}""");

            Thread.Sleep(2000);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Stream, Encoding.UTF8, "text/event-stream")
            };
        });

        string answer;
        using (CooksTurn())
            answer = await Tools(handler, timeout: TimeSpan.FromMilliseconds(200))
                .AskLookupAgent(Address, "FetchUrl", "a slow page");

        Assert.That(answer, Does.StartWith("Error:").And.Contain("didn't finish"));
        Assert.That(deleted, Is.True);
        Assert.That(LookupAsk.DefaultTimeout, Is.EqualTo(TimeSpan.FromMinutes(2)), "inside the subagent's 5");
    }

    /// <summary>Even in a chat a lookup agent is never kept: it's deleted as soon as it answers.</summary>
    [Test]
    public async Task InAChat_ALookupAgentIsNeverKept_ItIsDeletedOnceItAnswers()
    {
        // F30, the fourth hand check: a chat keeps three children, so kept lookup agents evicted the cook's earlier
        // ones and its calls to continue them failed. A lookup agent answers once and closes (Louis: "do all three").
        int deletes = 0;
        var handler = new StubHandler(request =>
        {
            if (request.Method == HttpMethod.Delete)
            {
                deletes++;
                return Json(HttpStatusCode.OK, "{}");
            }

            if (request.RequestUri!.AbsolutePath == "/v1/sessions")
                return Json(HttpStatusCode.Created, """{"session_id":"sess_lookup"}""");

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Stream, Encoding.UTF8, "text/event-stream")
            };
        });
        var registry = new SubSessionRegistry();

        string answer;
        using (CooksTurn())
        using (SubSessionRegistry.Use(registry))
            answer = await Tools(handler).AskLookupAgent(Address, "FetchUrl", "a page");

        Assert.That(answer, Does.StartWith("shop: Checkers").And.Not.Contain("handle"));
        Assert.That(registry.Open, Is.Empty, "the chat's three places stay for its subagents");
        Assert.That(deletes, Is.EqualTo(1));
        Assert.That(typeof(AskAgentTools).GetMethods().Select(m => m.Name),
            Has.None.StartsWith("Continue").And.None.StartsWith("Start"), "AskAgent alone: nothing to continue");
    }

    /// <summary>
    /// LOOKUP_LLMS gives each lookup agent, or the default, its models cheapest first; broken entries are dropped.
    /// </summary>
    [Test]
    public void TheLookupModels_AreParsedFromLookupLlms_RoleOrStar_CheapestFirst()
    {
        var llms = AgentOptions.ParseLookupLlms(
            " *=Ollama-Puls > instance , puls-lookup-web-summary=instance, broken, =x, puls-lookup-a=");

        Assert.That(llms["*"], Is.EqualTo(new[] { "ollama-puls", "instance" }));
        Assert.That(llms["puls-lookup-web-summary"], Is.EqualTo(new[] { "instance" }));
        Assert.That(llms.Keys, Is.EquivalentTo(new[] { "*", "puls-lookup-web-summary" }),
            "an entry without a role or a model is dropped");
        Assert.That(
            AgentOptions.FromEnvironment(name => name == "LOOKUP_LLMS" ? "*=ollama-puls>instance" : null)
                .LookupLlms["*"], Is.EqualTo(new[] { "ollama-puls", "instance" }));
        Assert.That(
            AgentOptions.FromEnvironment(name => name == "MINION_LLMS" ? "*=instance" : null).LookupLlms, Is.Empty,
            "the old name sets nothing (CL68.3, ADR 0049)");
        Assert.That(AgentOptions.FromEnvironment(_ => null).LookupLlms, Is.Empty, "unset: no lookup agents toolset");
    }

    /// <summary>A lookup agent model that is neither a known profile nor the instance's own stops start-up.</summary>
    [Test]
    public void ALookupAgentModel_ThatIsntAProfileOrTheInstance_StopsStartUp()
    {
        var agent = new AgentOptions
        {
            SessionLlmProfiles = ["ollama-puls"],
            LookupLlms = new Dictionary<string, IReadOnlyList<string>> { ["*"] = ["ollama-puls", "gpt-local"] }
        };

        var ex = Assert.Throws<SessionLlmException>(() => SessionLlmLoader.CheckLookupLlms(agent))!;

        Assert.That(ex.Message, Does.Contain("gpt-local").And.Contain("ollama-puls").And.Contain("instance"));
        Assert.DoesNotThrow(() => SessionLlmLoader.CheckLookupLlms(new AgentOptions
        {
            SessionLlmProfiles = ["ollama-puls"],
            LookupLlms = new Dictionary<string, IReadOnlyList<string>> { ["*"] = ["instance", "ollama-puls"] }
        }));
    }

    /// <summary>
    /// No lookup agents toolset any more: the agents toolset asks lookup agents too, and needs only its peers (a lookup
    /// agent without models is refused when it is asked). A tool still names the toolset it belongs to.
    /// </summary>
    [Test]
    public void TheLookupAgentsToolset_IsGone_TheAgentsToolsetAsksLookupAgents_AndATool_NamesItsToolset()
    {
        var agents = ToolsetCatalog.Find(CoreToolset.Agents.Wire())!;
        var without = new ToolsetContext(new AgentOptions { RatesUrl = null }, new WebParts(new NoSearch()));
        var with = new ToolsetContext(new AgentOptions
        {
            RatesUrl = null,
            AgentPeers = Self
        },
            new WebParts(new NoSearch()));

        Assert.That(ToolsetCatalog.All.Select(t => t.Name),
            Is.EqualTo(new[] { CoreToolset.Agents.Wire(), CoreToolset.Skills.Wire() }));
        Assert.That(agents.MissingSettings(without), Is.EqualTo(new[] { "AGENT_PEERS" }));
        Assert.That(agents.IsAvailable(with), Is.True);
        Assert.That(agents.GuideFile, Is.EqualTo("toolsets/agents.md"));
        Assert.That(TestTools.Catalog.ToolsetOf("FetchUrl"), Is.EqualTo(TestToolset.Web.Wire()));
        Assert.That(TestTools.Catalog.ToolsetOf("ReadWorkspaceFile"), Is.EqualTo(TestToolset.Files.Wire()));
        Assert.That(AgentCatalog.CoreOnly.ToolsetOf("CreateTool"), Is.Null, "engine-provided: no toolset builds it");
    }

    /// <summary>A search provider that finds nothing, so the toolsets can be checked without one.</summary>
    private sealed class NoSearch : IWebSearchProvider
    {
        /// <summary>It has no real name.</summary>
        public string Name => "none";

        /// <summary>Finds nothing.</summary>
        public Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchQuery query,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WebSearchResult>>([]);
    }
}
