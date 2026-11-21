using LouisAgent.Core.Hosting;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Telemetry;
using Serilog;

namespace LouisAgent.Core.Tests.Telemetry;

/// <summary>
/// The telemetry seam (CL33): a host with none gets the Null Object, which starts nothing and adds no sink; the
/// logging hands its configuration to whatever telemetry a host gives, so the collector's sink is the library's to
/// add; and core's own span source is the one the library will listen to.
/// </summary>
[NonParallelizable]
public class HostTelemetryTests
{
    [TearDown]
    public void TearDown() => AgentLogging.Configure(null);

    [Test]
    public void NoServices_MeansNoTelemetry_ThatStartsNothing()
    {
        var telemetry = HostServices.None.TelemetryOrNone;

        using var handle = telemetry.Start();
        telemetry.Flush();

        Assert.That(telemetry, Is.SameAs(NoTelemetry.Instance));
    }

    [Test]
    public void TheLogging_AsksTheTelemetryForItsSinks_OnEveryConfigure()
    {
        var sinks = new List<LoggerConfiguration>();

        AgentLogging.Configure(null, also: sinks.Add);
        AgentLogging.Flush();

        Assert.That(sinks, Has.Count.EqualTo(2), "configured, then made again by the flush");
    }

    [Test]
    public void CoreOwnsTheSpanSource_ByName() => Assert.That(TurnSpans.Source.Name, Is.EqualTo("louis-agent"));
}
