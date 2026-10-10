using System.Net;
using System.Text.Json;
using louis_agent.core.config;
using louis_agent.core.providers;
using louis_agent.core.tools;
using louis_agent.core.usage;

namespace louis_agent.orchestrator.tests;

// F1-S3: the orchestrator tags each fix session, so louis-agent's usage records carry the fix, the run and the service.
public class SessionTagsTests
{
    private string _dir = null!;

    [SetUp]
    public void SetUp() => _dir = Directory.CreateTempSubdirectory("session-tags-").FullName;

    [TearDown]
    public void TearDown() => Directory.Delete(_dir, recursive: true);

    [Test]
    public async Task CreateSession_WithTags_SendsThemAsTheBody()
    {
        var handler = new CapturingHandler();
        var client = new LouisAgentClient(new HttpClient(handler) { BaseAddress = new Uri("http://louis-agent/") }, null);

        string id = await client.CreateSessionAsync(new UsageTags("fix:e8b0b572223d", "run-20261010-160000", "order-service"));

        Assert.That(id, Is.EqualTo("sess_1"));
        Assert.That(handler.Path, Is.EqualTo("/sessions"));
        using var body = JsonDocument.Parse(handler.Body!);
        Assert.That(body.RootElement.GetProperty("task").GetString(), Is.EqualTo("fix:e8b0b572223d"));
        Assert.That(body.RootElement.GetProperty("run").GetString(), Is.EqualTo("run-20261010-160000"));
        Assert.That(body.RootElement.GetProperty("service").GetString(), Is.EqualTo("order-service"));
    }

    [Test]
    public async Task CreateSession_WithoutTags_SendsNoBody()
    {
        var handler = new CapturingHandler();
        var client = new LouisAgentClient(new HttpClient(handler) { BaseAddress = new Uri("http://louis-agent/") }, null);

        await client.CreateSessionAsync();

        Assert.That(handler.Body, Is.Null);
    }

    [Test]
    public void FixTags_NameTheErrorTheRunAndTheService()
    {
        ServiceTools tools = Tools();
        tools.RunId = "run-20261010-160000";

        Assert.That(tools.FixTags("e8b0b572223d"), Is.EqualTo(new UsageTags("fix:e8b0b572223d", "run-20261010-160000", "order-service")));
    }

    [Test]
    public void RunId_IsNotATool()
    {
        // Every public member of a toolset becomes a tool, property accessors included; RunId must stay internal.
        var engine = new AgentEngine(new CompositeSkillProvider(), new NoModel(), new AgentOptions { WorkspaceRoot = _dir },
            toolsets: [Tools()]);

        Assert.That(engine.Tools.Select(t => t.Name), Has.None.Contains("RunId"));
    }

    private ServiceTools Tools()
    {
        var options = new OrchestratorOptions
        {
            ServiceRoot = _dir, StateDirectory = Path.Combine(_dir, "state"), SkillsDirectory = Path.Combine(_dir, "Skills"),
        };
        var client = new LouisAgentClient(new HttpClient { BaseAddress = new Uri("http://127.0.0.1:1") }, null);
        return new ServiceTools(options, new ErrorFeed(Path.Combine(_dir, "errors.jsonl"), options.StateDirectory), client,
            new HashSet<string> { "order-total" }, new CommsLog(Path.Combine(_dir, "agent-comms.md")), _ => { });
    }

    /// <summary>Answers POST /sessions like louis-agent.api and keeps what was sent.</summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? Path { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Path = request.RequestUri!.AbsolutePath;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("""{"session_id":"sess_1"}""") };
        }
    }

    private sealed class NoModel : Microsoft.Extensions.AI.IChatClient
    {
        public Task<Microsoft.Extensions.AI.ChatResponse> GetResponseAsync(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
            Microsoft.Extensions.AI.ChatOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public IAsyncEnumerable<Microsoft.Extensions.AI.ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, Microsoft.Extensions.AI.ChatOptions? options = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }
}
