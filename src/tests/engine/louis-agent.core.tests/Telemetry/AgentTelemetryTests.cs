using System.Diagnostics;
using LouisAgent.Choices;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Telemetry;
using LouisAgent.Core.Tests.TestOtlp;
using LouisAgent.Core.Usage.Records;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Telemetry;

/// <summary>
/// With an OTLP endpoint set the standard way, a process's spans leave it for that collector (F52 step 2, ADR 0064):
/// every model request Microsoft.Extensions.AI traces, and anything opened on our own source, named for the host,
/// with the environment's service name winning over ours. Started once per process; with no endpoint, nothing
/// listens, so a host with no collector pays nothing for its spans.
/// </summary>
/// <remarks>
/// Not parallel: the <c>OTEL_*</c> variables and the tracer belong to the whole process. Each test sets the variables
/// and starts the tracer; the tear-down disposes it and clears them, so the next test starts fresh.
/// </remarks>
[NonParallelizable]
public class AgentTelemetryTests
{
    /// <summary>Where the exporter sends, the standard way.</summary>
    private const string EndpointVariable = "OTEL_EXPORTER_OTLP_ENDPOINT";

    /// <summary>How it sends: HTTP with protobuf, which a plain listener can take.</summary>
    private const string ProtocolVariable = "OTEL_EXPORTER_OTLP_PROTOCOL";

    /// <summary>The service name the environment gives, which wins over ours.</summary>
    private const string ServiceVariable = "OTEL_SERVICE_NAME";

    /// <summary>The operation tag every GenAI span carries (<c>chat</c> for a model request).</summary>
    private const string Operation = "gen_ai.operation.name";

    /// <summary>The source Microsoft.Extensions.AI traces model requests and tool calls on.</summary>
    private const string ModelSource = "Experimental.Microsoft.Extensions.AI";

    /// <summary>The source .NET's <see cref="HttpClient"/> traces its calls on.</summary>
    private const string HttpSource = "System.Net.Http";

    /// <summary>The collector the exporter sends to.</summary>
    private OtlpReceiver _receiver = null!;

    /// <summary>The tracer the test started, disposed after it.</summary>
    private IDisposable? _telemetry;

    /// <summary>A fresh collector, and the exporter pointed at it over HTTP.</summary>
    [SetUp]
    public void SetUp()
    {
        _receiver = new OtlpReceiver();
        Environment.SetEnvironmentVariable(EndpointVariable, _receiver.Endpoint);
        Environment.SetEnvironmentVariable(ProtocolVariable, "http/protobuf");
    }

    /// <summary>The tracer disposed and the variables cleared, so the next test starts from nothing.</summary>
    [TearDown]
    public void TearDown()
    {
        _telemetry?.Dispose();
        Environment.SetEnvironmentVariable(EndpointVariable, null);
        Environment.SetEnvironmentVariable(ProtocolVariable, null);
        Environment.SetEnvironmentVariable(ServiceVariable, null);
        _receiver.Dispose();
    }

    /// <summary>A model request in a turn reaches the collector as a span, named for the host.</summary>
    [Test]
    public async Task AModelRequest_ReachesTheCollector_AsASpan()
    {
        _telemetry = AgentTelemetry.Start();

        await OneTurnAsync();
        AgentTelemetry.Flush();

        Assert.That(await _receiver.EventuallyHoldsAsync(Operation), Is.True, "no span reached the collector");
        Assert.That(_receiver.Received.Select(r => r.Path), Does.Contain("/v1/traces"));
        Assert.That(_receiver.Holds("service.name"), Is.True);
        Assert.That(_receiver.Holds(AgentLogging.ServiceName), Is.True, "the default service name");
    }

    /// <summary><c>OTEL_SERVICE_NAME</c> names the service instead of ours: the environment comes after us.</summary>
    [Test]
    public async Task TheEnvironmentsServiceName_Wins()
    {
        Environment.SetEnvironmentVariable(ServiceVariable, "my-host");
        _telemetry = AgentTelemetry.Start();

        await OneTurnAsync();
        AgentTelemetry.Flush();

        Assert.That(await _receiver.EventuallyHoldsAsync("my-host"), Is.True);
    }

    /// <summary>The tracer is the process's one: a second start hands back the same handle.</summary>
    [Test]
    public void ASecondStart_GivesTheSameHandle()
    {
        _telemetry = AgentTelemetry.Start();

        Assert.That(AgentTelemetry.Start(), Is.SameAs(_telemetry));
    }

    /// <summary>With no endpoint, nothing listens to the spans and nothing is sent.</summary>
    [Test]
    public async Task NoEndpoint_ListensToNothing_AndSendsNothing()
    {
        Environment.SetEnvironmentVariable(EndpointVariable, null);
        _telemetry = AgentTelemetry.Start();

        await OneTurnAsync();
        AgentTelemetry.Flush();
        await Task.Delay(200);

        using var model = new ActivitySource(ModelSource);
        Assert.That(model.HasListeners(), Is.False, "a host with no collector pays for its spans");
        Assert.That(_receiver.Received, Is.Empty);
    }

    /// <summary>A span on our own source reaches the collector too: step 3's turn span opens there.</summary>
    [Test]
    public async Task OurSource_IsListenedTo()
    {
        _telemetry = AgentTelemetry.Start();

        using (AgentTelemetry.Source.StartActivity("our-own-span"))
        {
        }

        AgentTelemetry.Flush();

        Assert.That(await _receiver.EventuallyHoldsAsync("our-own-span"), Is.True);
    }

    /// <summary>
    /// A call to the collector itself is no span of ours (F52 step 4b, H1): the log sink sends its batches on its own
    /// client, and without the filter every batch of log lines would be a span. Any other call stays.
    /// </summary>
    [Test]
    public async Task ACallToTheCollector_IsLeftOut_AnyOtherStays()
    {
        _telemetry = AgentTelemetry.Start();

        // Two calls as HttpClient's source reports them: one to the collector, as the log sink makes, one elsewhere.
        using (var http = new ActivitySource(HttpSource))
        {
            using (var own = http.StartActivity("collector-call"))
                own?.SetTag("url.full", _receiver.Endpoint + "/v1/logs");
            using (var other = http.StartActivity("other-call"))
                other?.SetTag("url.full", "http://example.test/other");
        }

        AgentTelemetry.Flush();

        Assert.That(await _receiver.EventuallyHoldsAsync("other-call"), Is.True, "another call is still a span");
        Assert.That(_receiver.Holds("collector-call"), Is.False, "the collector's own traffic became a span");
    }

    /// <summary>One turn with one model request, as <c>ChatPipelineTests</c> runs it, on a fake model.</summary>
    private static async Task OneTurnAsync()
    {
        var client = new FakeChatClient(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hallo")));
        var engine = new AgentEngine(
            new MarkdownSkillProvider(TestPaths.SampleSkills), client,
            TestPaths.Agent(), toolsets: TestPaths.Toolsets(TestPaths.Agent(), [TestToolset.Files.Wire()]),
            usageSink: new ListUsageSink());

        using (UsageScope.Begin())
            await engine.ProcessPromptAsync(engine.NewHistory(), "Hallo?");
    }
}
