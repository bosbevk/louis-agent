using LouisAgent.Composition.Logging;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Logging.Messages;
using LouisAgent.Core.Tests.TestOtlp;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Core.Tests.Logging;

/// <summary>
/// With an OTLP endpoint set the standard way (<c>OTEL_EXPORTER_OTLP_ENDPOINT</c>), every log line also leaves the
/// process for that collector, named for its host, its template's holes as fields (F52 step 1, ADR 0064); the
/// JSON-lines records stay out, as they do on the console; with no endpoint nothing is sent.
/// </summary>
/// <remarks>
/// Not parallel: the <c>OTEL_*</c> variables and the logging configuration belong to the whole process. Each test
/// sets them, configures, and the tear-down clears them and configures again, so no other test sees the sink.
/// </remarks>
[NonParallelizable]
public class AgentLoggingOtlpTests
{
    /// <summary>Where the sink sends, the standard way.</summary>
    private const string EndpointVariable = "OTEL_EXPORTER_OTLP_ENDPOINT";

    /// <summary>
    /// How it sends: HTTP with protobuf, which a plain listener can take (the sink's default is gRPC).
    /// </summary>
    private const string ProtocolVariable = "OTEL_EXPORTER_OTLP_PROTOCOL";

    /// <summary>The service name the environment gives, which wins over ours.</summary>
    private const string ServiceVariable = "OTEL_SERVICE_NAME";

    /// <summary>The collector the sink sends to.</summary>
    private OtlpReceiver _receiver = null!;

    /// <summary>A fresh collector, and the sink pointed at it over HTTP.</summary>
    [SetUp]
    public void SetUp()
    {
        _receiver = new OtlpReceiver();
        Environment.SetEnvironmentVariable(EndpointVariable, _receiver.Endpoint);
        Environment.SetEnvironmentVariable(ProtocolVariable, "http/protobuf");
    }

    /// <summary>The variables cleared and logging configured again without them, so the sink is gone.</summary>
    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable(EndpointVariable, null);
        Environment.SetEnvironmentVariable(ProtocolVariable, null);
        Environment.SetEnvironmentVariable(ServiceVariable, null);
        AgentLogging.Configure(null);
        _receiver.Dispose();
    }

    /// <summary>A line reaches the collector with the host's service name and its template's hole as a field.</summary>
    [Test]
    public async Task ALine_ReachesTheCollector_WithTheServiceName_AndItsFields()
    {
        AgentLogging.Configure(null);

        LogOneLine();
        AgentLogging.Flush();

        Assert.That(await _receiver.EventuallyHoldsAsync("service.name"), Is.True, "nothing reached the collector");
        Assert.That(_receiver.Holds(AgentLogging.ServiceName), Is.True, "the default service name");
        Assert.That(_receiver.Holds("Path"), Is.True, "the template's hole, as a field");
        // Given the base address, the sink adds the logs' own path, as the spec says for the shared endpoint.
        Assert.That(_receiver.Received.Select(r => r.Path).Distinct(), Is.EqualTo(new[] { "/v1/logs" }));
    }

    /// <summary>
    /// A pack writes through the contract alone and knows nothing of Serilog or OTLP, and its line still reaches the
    /// collector, because the engine decides where lines go.
    /// </summary>
    [Test]
    public async Task APacksLine_ReachesTheCollector_ThroughTheContract()
    {
        AgentLogging.Configure(null);

        PackLog.For<AgentLoggingOtlpTests>().Log(LogLevel.Information, LoggingLogMessage.LoggingTo,
            new { Path = "from-a-pack" });
        AgentLogging.Flush();

        Assert.That(await _receiver.EventuallyHoldsAsync("from-a-pack"), Is.True, "the pack's line never arrived");
    }

    /// <summary><c>OTEL_SERVICE_NAME</c> names the service instead of ours, as the spec says it wins.</summary>
    [Test]
    public async Task TheEnvironmentsServiceName_Wins()
    {
        Environment.SetEnvironmentVariable(ServiceVariable, "my-host");
        AgentLogging.Configure(null);

        LogOneLine();
        AgentLogging.Flush();

        Assert.That(await _receiver.EventuallyHoldsAsync("my-host"), Is.True);
    }

    /// <summary>A record for a JSON-lines file stays out of the collector, as it stays off the console.</summary>
    [Test]
    public async Task ARecord_StaysOut()
    {
        AgentLogging.Configure(null);

        // The record as the tool loop writes it, then a plain line, so the plain one proves the batch went out.
        ToolCallRecords.TruncatedToolCall("WriteWorkspaceFile", "{}", 16000);
        LogOneLine();
        AgentLogging.Flush();

        Assert.That(await _receiver.EventuallyHoldsAsync("Path"), Is.True, "the plain line still went");
        Assert.That(_receiver.Holds("MaxOutputTokens"), Is.False, "the record went too");
    }

    /// <summary>With no endpoint set, nothing is sent anywhere.</summary>
    [Test]
    public async Task NoEndpoint_SendsNothing()
    {
        Environment.SetEnvironmentVariable(EndpointVariable, null);
        AgentLogging.Configure(null);

        LogOneLine();
        AgentLogging.Flush();
        await Task.Delay(200);

        Assert.That(_receiver.Received, Is.Empty);
    }

    /// <summary>One plain line with one hole, <c>Path</c>.</summary>
    private static void LogOneLine() =>
        AgentLogging.For<AgentLoggingOtlpTests>().Log(LogLevel.Information, LoggingLogMessage.LoggingTo,
            new { Path = "x" });
}
