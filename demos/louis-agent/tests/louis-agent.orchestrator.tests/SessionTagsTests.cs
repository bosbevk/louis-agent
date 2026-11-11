using System.Net;
using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Orchestrator.Client;
using LouisAgent.Orchestrator.Comms;
using LouisAgent.Orchestrator.Tools;
using LouisAgent.Packs.Triage.Feed;

namespace LouisAgent.Orchestrator.Tests;

/// <summary>
/// The orchestrator tags each fix session, so louis-agent's usage records carry the fix, the run and the service
/// (F1-S3).
/// </summary>
public class SessionTagsTests
{
    /// <summary>This test's folder, made fresh and removed after.</summary>
    private string _dir = null!;

    /// <summary>A fresh folder for each test.</summary>
    [SetUp]
    public void SetUp() => _dir = Directory.CreateTempSubdirectory("session-tags-").FullName;

    /// <summary>Removes the folder.</summary>
    [TearDown]
    public void TearDown() => Directory.Delete(_dir, recursive: true);

    /// <summary>A fix session's tags go in the body that opens it.</summary>
    [Test]
    public async Task CreateSession_WithTags_SendsThemAsTheBody()
    {
        var handler = new CapturingHandler();
        var client = new LouisAgentClient(new HttpClient(handler) { BaseAddress = new Uri("http://louis-agent/") },
            null);

        string id = await client.CreateSessionAsync(new UsageTags("fix:e8b0b572223d", "run-20261010-160000",
            "order-service"));

        Assert.That(id, Is.EqualTo("sess_1"));
        Assert.That(handler.Path, Is.EqualTo("/v1/sessions"));
        using var body = JsonDocument.Parse(handler.Body!);
        Assert.That(body.RootElement.GetProperty("task").GetString(), Is.EqualTo("fix:e8b0b572223d"));
        Assert.That(body.RootElement.GetProperty("run").GetString(), Is.EqualTo("run-20261010-160000"));
        Assert.That(body.RootElement.GetProperty("service").GetString(), Is.EqualTo("order-service"));
    }

    /// <summary>A fix loads the coding toolsets only, and they go with the session (F5-S4).</summary>
    [Test]
    public async Task CreateSession_WithToolsets_SendsThem()
    {
        var handler = new CapturingHandler();
        var client = new LouisAgentClient(new HttpClient(handler) { BaseAddress = new Uri("http://louis-agent/") },
            null);

        await client.CreateSessionAsync(new UsageTags("fix:1", "run-1", "order-service"),
            toolsets: ServiceTools.FixToolsets);

        using var body = JsonDocument.Parse(handler.Body!);
        Assert.That(body.RootElement.GetProperty("toolsets").EnumerateArray().Select(t => t.GetString()),
            Is.EqualTo(new[]
            {
                FixToolset.DotNet.Wire(), FixToolset.Files.Wire(), FixToolset.Git.Wire(), FixToolset.Skills.Wire()
            }), "F5-S4: a fix loads the coding profile only");
    }

    /// <summary>With no tags and no toolsets there's nothing to say, so no body goes.</summary>
    [Test]
    public async Task CreateSession_WithoutTags_SendsNoBody()
    {
        var handler = new CapturingHandler();
        var client = new LouisAgentClient(new HttpClient(handler) { BaseAddress = new Uri("http://louis-agent/") },
            null);

        await client.CreateSessionAsync();

        Assert.That(handler.Body, Is.Null);
    }

    /// <summary>A fix's tags name its error, the run and the service.</summary>
    [Test]
    public void FixTags_NameTheErrorTheRunAndTheService()
    {
        ServiceTools tools = Tools();
        tools.RunId = "run-20261010-160000";

        Assert.That(tools.FixTags("e8b0b572223d"),
            Is.EqualTo(new UsageTags("fix:e8b0b572223d", "run-20261010-160000", "order-service")));
    }

    /// <summary>
    /// The run id is no tool: every public member of a toolset becomes one, property accessors included, so RunId
    /// stays internal.
    /// </summary>
    [Test]
    public void RunId_IsNotATool()
    {
        var engine = new AgentEngine(new CompositeSkillProvider(), new NoModel(),
            new AgentOptions { WorkspaceRoot = _dir },
            toolsets: [Tools()]);

        Assert.That(engine.Tools.Select(t => t.Name), Has.None.Contains("RunId"));
    }

    /// <summary>The orchestrator's tools over this folder, with a client pointed at nothing.</summary>
    private ServiceTools Tools()
    {
        var options = new OrchestratorOptions
        {
            ServiceRoot = _dir,
            StateDirectory = Path.Combine(_dir, "state"),
            SkillsDirectory = Path.Combine(_dir, "Skills"),
        };
        var client = new LouisAgentClient(new HttpClient { BaseAddress = new Uri("http://127.0.0.1:1") }, null);
        return new ServiceTools(options,
            new JsonLinesErrorFeed(Path.Combine(_dir, "errors.jsonl"), options.StateDirectory),
            client,
            new HashSet<string> { "order-total" }, new CommsLog(Path.Combine(_dir, "agent-comms.md")));
    }

    /// <summary>Answers POST /v1/sessions like louis-agent.api and keeps what was sent.</summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        /// <summary>The path it was last sent to.</summary>
        public string? Path { get; private set; }

        /// <summary>The body it was last sent, if any.</summary>
        public string? Body { get; private set; }

        /// <summary>Keeps what was sent and answers that sess_1 is open.</summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Path = request.RequestUri!.AbsolutePath;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("""{"session_id":"sess_1"}""")
            };
        }
    }

    /// <summary>A model that is never asked: the test only builds an engine to list its tools.</summary>
    private sealed class NoModel : Microsoft.Extensions.AI.IChatClient
    {
        /// <summary>Never asked.</summary>
        public Task<Microsoft.Extensions.AI.ChatResponse> GetResponseAsync(
            IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
            Microsoft.Extensions.AI.ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        /// <summary>Never asked.</summary>
        public IAsyncEnumerable<Microsoft.Extensions.AI.ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
            Microsoft.Extensions.AI.ChatOptions? options = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        /// <summary>Offers nothing.</summary>
        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        /// <summary>Holds nothing to let go of.</summary>
        public void Dispose()
        {
        }
    }
}
