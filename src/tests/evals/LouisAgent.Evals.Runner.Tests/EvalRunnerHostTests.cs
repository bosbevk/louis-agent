using LouisAgent.Core.Hosting;
using LouisAgent.Core.Telemetry;
using LouisAgent.Evals.Runner.Catalog;
using Serilog;

namespace LouisAgent.Evals.Runner.Tests;

/// <summary>
/// The runner starts the telemetry its program hands in (CL57), and none when the program hands in nothing: a
/// library decides nothing about the world, the way CL33 took the tracer out of core.
/// </summary>
[NonParallelizable]
public class EvalRunnerHostTests
{
    private sealed class RecordingTelemetry : IHostTelemetry
    {
        public int Started { get; private set; }

        public void AddSinks(LoggerConfiguration logging)
        {
        }

        public IDisposable Start()
        {
            Started++;
            return new NoTelemetryHandle();
        }

        public void Flush()
        {
        }

        private sealed class NoTelemetryHandle : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    [Test]
    public async Task TheTelemetryTheProgramGave_IsStarted()
    {
        var telemetry = new RecordingTelemetry();

        int exit = await EvalRunnerHost.RunAsync(["--list"], RunnerCatalogs.GeneralOnly,
            new HostServices(Telemetry: telemetry));

        Assert.That((exit, telemetry.Started), Is.EqualTo((0, 1)));
    }

    [Test]
    public async Task NoServices_StillLists() =>
        Assert.That(await EvalRunnerHost.RunAsync(["--list"], RunnerCatalogs.GeneralOnly), Is.Zero);
}
