using LouisAgent.Testing;
using LouisAgent.Choices;
using LouisAgent.Core.Hosting.SessionLlms;
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
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Tools.Agents;
using LouisAgent.Core.Tools.Web.Search;

namespace LouisAgent.Core.Tests.Tools.Agents;

/// <summary>
/// A specialist hands one task and one of its own tools to a minion on a cheap model (F30 step 5; MinionS: small
/// single-step tasks for the local model, the cloud model coordinates). The peer is a stub: no network, no model.
/// </summary>
public class MinionToolsTests
{
    /// <summary>The minion role every test hands work to.</summary>
    private const string Role = SampleRole.Zeta;

    /// <summary>This instance as its own peer, where a minion's session opens.</summary>
    private static readonly Dictionary<string, Uri> Self = new()
    {
        [SampleIdentity.Alpha] = new Uri("http://puls-agent.test/")
    };

    /// <summary>Every role's models, cheapest first: the local model, then the instance's own.</summary>
    private static readonly Dictionary<string, IReadOnlyList<string>> Cascade = new()
    {
        ["*"] = ["ollama-puls", "instance"]
    };

    /// <summary>A minion's answer: one shop and its price, and what it used on the local model.</summary>
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
    /// The specialist's own instance: it opens sess_minion, answers with the stream and accepts the delete, and keeps
    /// every body it was asked to open a session with.
    /// </summary>
    private static StubHandler Peer(List<JsonDocument> creates, Func<HttpResponseMessage>? answer = null) =>
        new(request =>
        {
            if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath == "/v1/sessions")
            {
                creates.Add(JsonDocument.Parse(request.Content!.ReadAsStringAsync().Result));
                return Json(HttpStatusCode.Created,
                    SampleRole.Fill("""{"session_id":"sess_minion","role":"<RoleZeta>"}"""));
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

    /// <summary>The minions toolset on the stub peer, with the default models unless a test gives its own.</summary>
    private static MinionTools Tools(StubHandler handler,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? llms = null, TimeSpan? timeout = null,
        IUsageSink? sink = null) =>
        new(Self, llms ?? Cascade, "the-key", new HttpClient(handler), timeout, usageSink: sink);

    /// <summary>The cook's turn: its tools as the host sets them, under Puls's turn as the parent.</summary>
    private static IDisposable CooksTurn(string session = "sess_cook", int turn = 2, string parent = "sess_puls#1",
        params string[] tools)
    {
        var scope = UsageScope.Begin(host: "api", session: session, turn: turn,
            tags: new UsageTags(null, null, null, parent));
        var mine = AgentTools.Use(tools.Length == 0
            ? ["FetchUrl", "ReadRenderedPage", "WebSearch", "ExecuteSkill"]
            : tools);
        return new Both(scope, mine);
    }

    /// <summary>Two scopes that end together, the second one first.</summary>
    private sealed class Both(IDisposable first, IDisposable second) : IDisposable
    {
        /// <summary>Ends both scopes, in the reverse of the order they began.</summary>
        public void Dispose()
        {
            second.Dispose();
            first.Dispose();
        }
    }

    /// <summary>
    /// A minion opens two levels down with its role, its one tool and the role's model, and what it used is rolled up
    /// under the specialist's turn.
    /// </summary>
    [Test]
    public async Task AMinion_OpensAtDepthTwo_WithItsRoleItsToolAndTheRolesModel_AndRollsUpUnderTheSpecialist()
    {
        var creates = new List<JsonDocument>();
        var sink = new ListUsageSink();
        string answer;
        using (CooksTurn())
            answer = await Tools(Peer(creates), sink: sink).StartMinion(Role, "FetchUrl",
                "https://shop.test/mince; beef mince 500 g; Stellenbosch");

        var body = creates.Single().RootElement;
        Assert.That(body.GetProperty("depth").GetInt32(), Is.EqualTo(2));
        Assert.That(body.GetProperty("role").GetString(), Is.EqualTo(Role), "CL14: the body says role");
        Assert.That(body.GetProperty("tool").GetString(), Is.EqualTo("FetchUrl"));
        Assert.That(body.GetProperty("llm").GetString(), Is.EqualTo("ollama-puls"),
            "the cheapest model in the role's list first");
        Assert.That(body.GetProperty("parent").GetString(), Is.EqualTo("sess_cook#2"),
            "the specialist's turn, so the ledger chains Puls → cook → minion");
        Assert.That(body.GetProperty("task").GetString(), Is.EqualTo($"minion:{Role}"));
        Assert.That(body.TryGetProperty("toolsets", out _), Is.False, "the peer derives the toolset from the tool");

        Assert.That(answer,
            Does.StartWith("shop: Checkers").And
                .Contain(SampleRole.Fill("[Minion <RoleZeta> on ollama-puls with FetchUrl")));
        UsageRecord row = sink.Records.Single();
        Assert.That((row.Purpose, row.Model, row.Session, row.Turn, row.Parent),
            Is.EqualTo(("sub_session", $"minion:{Role}", "sess_cook", (int?)2, "sess_puls#1")));
        Assert.That(row.Cost, Is.EqualTo(new UsageCost("USD", 0m, null)),
            "the local model's cost, 0, is still a number: never unknown");
    }

    /// <summary>
    /// A minion's answer reads the same whatever it used: on a replay its usage is zero, and the specialist's next
    /// request must still match its recording (FX27).
    /// </summary>
    [Test]
    public async Task TheAnswer_ReadsTheSame_WhateverTheMinionUsed()
    {
        string replayedStream = Stream.Replace("\"input_tokens\":1500", "\"input_tokens\":0")
            .Replace("\"output_tokens\":40", "\"output_tokens\":0");

        string recorded = await AnswerFrom(Stream);
        string replayed = await AnswerFrom(replayedStream);

        Assert.That(replayed, Is.EqualTo(recorded));
    }

    /// <summary>What the specialist reads back from one minion whose stream is <paramref name="stream"/>.</summary>
    private static async Task<string> AnswerFrom(string stream)
    {
        var answer = () => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(stream, Encoding.UTF8, "text/event-stream")
        };
        using (CooksTurn())
            return await Tools(Peer([], answer)).StartMinion(Role, "FetchUrl", "https://shop.test/mince");
    }

    /// <summary>A minion records into, or replays from, its chat's own trial; with no trial it gets none.</summary>
    [Test]
    public async Task TheReplay_GoesToTheMinion_AsTheParentsOwnTrial_AndNoneWhenThereIsNone()
    {
        // F18 step 4a: a minion that reads a page records into, or replays from, the trial of the chat it works for.
        var withReplay = new List<JsonDocument>();
        using (SessionReplay.Use(new SessionReplay(WebReplayMode.Replay,
                   SampleMode.Fill("run_1:<ModeBeta>:1/bobotie"), Model: false)))
        using (CooksTurn())
            await Tools(Peer(withReplay))
                .StartMinion(Role, "FetchUrl", "https://shop.test/mince; beef mince 500 g; Stellenbosch");
        var without = new List<JsonDocument>();
        using (CooksTurn())
            await Tools(Peer(without))
                .StartMinion(Role, "FetchUrl", "https://shop.test/mince; beef mince 500 g; Stellenbosch");

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
    /// A tool the specialist doesn't hold, one never handed down, or a role that isn't a minion's is refused before
    /// anything is sent.
    /// </summary>
    [Test]
    public async Task
        AToolTheSpecialistDoesntHold_OrNeverHandedDown_OrARoleThatIsntAMinions_IsRefused_WithoutANetworkCall()
    {
        var creates = new List<JsonDocument>();
        var tools = Tools(Peer(creates));

        using (CooksTurn(tools: ["FetchUrl"]))
        {
            Assert.That(await tools.StartMinion(Role, "ReadRenderedPage", "a page"),
                Does.StartWith("Error:").And.Contain("ReadRenderedPage").And.Contain("FetchUrl"),
                "only one of its own: the parent's toolbox is the ceiling");
            Assert.That(await tools.StartMinion(Role, "ExecuteSkill", "a page"),
                Does.StartWith("Error:").And.Contain("never"));
            Assert.That(await tools.StartMinion(SampleRole.Alpha, "FetchUrl", "a page"),
                Does.StartWith("Error:").And.Contain(SampleRole.Alpha), "a specialist's role is no minion's");
            Assert.That(await tools.StartMinion("puls-minion", "FetchUrl", "a page"), Does.StartWith("Error:"),
                "the base is no role");
        }

        Assert.That(creates, Is.Empty);
    }

    /// <summary>
    /// Asking for a stronger model runs the task on the next one in the list, and is refused past its end.
    /// </summary>
    [Test]
    public async Task Stronger_RunsTheSameTaskOnTheNextModel_AndIsRefusedAtTheEndOfTheList()
    {
        // FrugalGPT's cascade with VerifyQuote as the check: the specialist asks for the next model, never names one.
        var creates = new List<JsonDocument>();
        var tools = Tools(Peer(creates));

        using (CooksTurn())
        {
            await tools.StartMinion(Role, "FetchUrl", "a page", stronger: true);
            Assert.That(
                creates.Single().RootElement.TryGetProperty("llm", out var llm) &&
                llm.ValueKind == JsonValueKind.String, Is.False,
                "'instance' is the specialist's own model: no llm in the body");

            var oneModel = Tools(Peer(creates),
                new Dictionary<string, IReadOnlyList<string>> { ["*"] = ["ollama-puls"] });
            string refused = await oneModel.StartMinion(Role, "FetchUrl", "a page", stronger: true);
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
            await Tools(Peer(creates), llms).StartMinion(Role, "FetchUrl", "a page");
            Assert.That(
                creates.Single().RootElement.TryGetProperty("llm", out var llm) &&
                llm.ValueKind == JsonValueKind.String, Is.False);

            string refused = await Tools(Peer(creates), llms)
                .StartMinion("puls-minion-web-summary", "FetchUrl", "a page");
            Assert.That(refused, Does.StartWith("Error:").And.Contain("MINION_LLMS"),
                "no list for the role and no default");
            Assert.That(creates, Has.Count.EqualTo(1));
        }
    }

    /// <summary>
    /// One minion past the cap is refused, counted across every specialist of the same user turn; the next turn starts
    /// fresh.
    /// </summary>
    [Test]
    public async Task OnePastTheCap_IsRefused_AcrossSpecialists()
    {
        // One GPU answers one request at a time, so the limit counts the whole tree of Puls's turn: two specialists
        // under the same Puls turn share it. Every shop's price goes through a minion (Louis, F30 hand check), so a
        // meal of about eight ingredients at two shops needs sixteen.
        Assert.That(MinionTools.MaxPerTurn, Is.EqualTo(16));
        var creates = new List<JsonDocument>();
        var tools = Tools(Peer(creates));
        int first = MinionTools.MaxPerTurn - 2;

        using (CooksTurn(session: "sess_cook", turn: 1, parent: "sess_puls#4"))
            for (int i = 0; i < first; i++)
                Assert.That(await tools.StartMinion(Role, "FetchUrl", $"page {i}"), Does.Not.StartWith("Error:"));
        using (CooksTurn(session: "sess_researcher", turn: 1, parent: "sess_puls#4"))
        {
            for (int i = 0; i < 2; i++)
                Assert.That(await tools.StartMinion(Role, "FetchUrl", $"page {i}"), Does.Not.StartWith("Error:"));
            Assert.That(await tools.StartMinion(Role, "FetchUrl", "one more"),
                Does.StartWith("Error:").And.Contain("16"));
        }

        using (CooksTurn(session: "sess_cook", turn: 1, parent: "sess_puls#5"))
            Assert.That(await tools.StartMinion(Role, "FetchUrl", "next turn"), Does.Not.StartWith("Error:"),
                "another user turn starts fresh");

        Assert.That(creates, Has.Count.EqualTo(MinionTools.MaxPerTurn + 1));
    }

    /// <summary>A minion that takes too long is stopped after its own timeout and deleted on the peer.</summary>
    [Test]
    public async Task AMinion_IsStoppedAfterItsOwnTimeout_AndDeleted()
    {
        var creates = new List<JsonDocument>();
        var deleted = false;
        var handler = new StubHandler(request =>
        {
            if (request.Method == HttpMethod.Delete)
            {
                deleted = true;
                return Json(HttpStatusCode.OK, "{}");
            }

            if (request.RequestUri!.AbsolutePath == "/v1/sessions")
            {
                creates.Add(JsonDocument.Parse(request.Content!.ReadAsStringAsync().Result));
                return Json(HttpStatusCode.Created, """{"session_id":"sess_minion"}""");
            }

            Thread.Sleep(2000);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Stream, Encoding.UTF8, "text/event-stream")
            };
        });

        string answer;
        using (CooksTurn())
            answer = await Tools(handler, timeout: TimeSpan.FromMilliseconds(200))
                .StartMinion(Role, "FetchUrl", "a slow page");

        Assert.That(answer, Does.StartWith("Error:").And.Contain("didn't finish"));
        Assert.That(deleted, Is.True);
        Assert.That(MinionTools.DefaultTimeout, Is.EqualTo(TimeSpan.FromMinutes(2)), "inside the specialist's 5");
    }

    /// <summary>Even in a chat a minion is never kept: it's deleted as soon as it answers.</summary>
    [Test]
    public async Task InAChat_AMinionIsNeverKept_ItIsDeletedOnceItAnswers()
    {
        // F30, the fourth hand check: a chat keeps three children, so kept minions evicted the cook's earlier ones and
        // its ContinueMinion calls failed. A minion answers once and closes (Louis: "do all three").
        var creates = new List<JsonDocument>();
        int deletes = 0;
        var handler = new StubHandler(request =>
        {
            if (request.Method == HttpMethod.Delete)
            {
                deletes++;
                return Json(HttpStatusCode.OK, "{}");
            }

            if (request.RequestUri!.AbsolutePath == "/v1/sessions")
            {
                creates.Add(JsonDocument.Parse(request.Content!.ReadAsStringAsync().Result));
                return Json(HttpStatusCode.Created, """{"session_id":"sess_minion"}""");
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Stream, Encoding.UTF8, "text/event-stream")
            };
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

    /// <summary>
    /// MINION_LLMS gives each role, or the default, its models cheapest first; broken entries are dropped.
    /// </summary>
    [Test]
    public void TheMinionModels_AreParsedFromMinionLlms_RoleOrStar_CheapestFirst()
    {
        var llms = AgentOptions.ParseMinionLlms(
            " *=Ollama-Puls > instance , puls-minion-web-summary=instance, broken, =x, puls-minion-a=");

        Assert.That(llms["*"], Is.EqualTo(new[] { "ollama-puls", "instance" }));
        Assert.That(llms["puls-minion-web-summary"], Is.EqualTo(new[] { "instance" }));
        Assert.That(llms.Keys, Is.EquivalentTo(new[] { "*", "puls-minion-web-summary" }),
            "an entry without a role or a model is dropped");
        Assert.That(
            AgentOptions.FromEnvironment(name => name == "MINION_LLMS" ? "*=ollama-puls>instance" : null)
                .MinionLlms["*"], Is.EqualTo(new[] { "ollama-puls", "instance" }));
        Assert.That(AgentOptions.FromEnvironment(_ => null).MinionLlms, Is.Empty, "unset: no minions toolset");
    }

    /// <summary>A minion model that is neither a known profile nor the instance's own stops start-up.</summary>
    [Test]
    public void AMinionModel_ThatIsntAProfileOrTheInstance_StopsStartUp()
    {
        var agent = new AgentOptions
        {
            SessionLlmProfiles = ["ollama-puls"],
            MinionLlms = new Dictionary<string, IReadOnlyList<string>> { ["*"] = ["ollama-puls", "gpt-local"] }
        };

        var ex = Assert.Throws<SessionLlmException>(() => SessionLlmLoader.CheckMinionLlms(agent))!;

        Assert.That(ex.Message, Does.Contain("gpt-local").And.Contain("ollama-puls").And.Contain("instance"));
        Assert.DoesNotThrow(() => SessionLlmLoader.CheckMinionLlms(new AgentOptions
        {
            SessionLlmProfiles = ["ollama-puls"],
            MinionLlms = new Dictionary<string, IReadOnlyList<string>> { ["*"] = ["instance", "ollama-puls"] }
        }));
    }

    /// <summary>The minions toolset needs peers and models, and a tool names the toolset it belongs to.</summary>
    [Test]
    public void TheMinionsToolset_NeedsPeersAndModels_AndATool_NamesItsToolset()
    {
        var minions = ToolsetCatalog.Find(CoreToolset.Minions.Wire())!;
        var without = new ToolsetContext(new AgentOptions { RatesUrl = null }, new NoSearch());
        var with = new ToolsetContext(new AgentOptions
        {
            RatesUrl = null,
            AgentPeers = Self,
            MinionLlms = Cascade
        },
            new NoSearch());

        Assert.That(minions.MissingSettings(without), Is.EqualTo(new[] { "AGENT_PEERS", "MINION_LLMS" }));
        Assert.That(minions.IsAvailable(with), Is.True);
        Assert.That(minions.GuideFile, Is.EqualTo("toolsets/minions.md"));
        Assert.That(AgentCatalog.CoreOnly.ToolsetOf("FetchUrl"), Is.EqualTo(CoreToolset.Web.Wire()));
        Assert.That(AgentCatalog.CoreOnly.ToolsetOf("ReadWorkspaceFile"), Is.EqualTo(CoreToolset.Files.Wire()));
        Assert.That(AgentCatalog.CoreOnly.ToolsetOf("CreateTool"), Is.Null, "engine-provided: no toolset builds it");
    }

    /// <summary>A search provider that finds nothing, so the toolsets can be checked without one.</summary>
    private sealed class NoSearch : LouisAgent.Core.Tools.Web.Search.IWebSearchProvider
    {
        /// <summary>It has no real name.</summary>
        public string Name => "none";

        /// <summary>Finds nothing.</summary>
        public Task<IReadOnlyList<LouisAgent.Core.Tools.Web.Search.WebSearchResult>> SearchAsync(
            LouisAgent.Core.Tools.Web.Search.WebSearchQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<LouisAgent.Core.Tools.Web.Search.WebSearchResult>>([]);
    }
}
