using Serilog;

namespace LouisAgent.Core.Telemetry;

/// <summary>
/// A host with no telemetry library: nothing listens to the spans, no sink is added, and a crash is still logged
/// once by <see cref="LastChance"/>, collector or not.
/// </summary>
/// <remarks>
/// Null Object: <c>AgentHost</c> holds one of these when a host hands it no telemetry, and asks it the same
/// questions, so there is no "if there is a tracer" branch in the bootstrap.
/// </remarks>
public sealed class NoTelemetry : IHostTelemetry
{
    public static readonly NoTelemetry Instance = new();

    private NoTelemetry()
    {
    }

    public void AddSinks(LoggerConfiguration logging)
    {
    }

    /// <summary>The crash handler goes in, as with a tracer; the handle has nothing to stop.</summary>
    public IDisposable Start()
    {
        LastChance.Install(this);
        return Nothing.Instance;
    }

    public void Flush()
    {
    }

    /// <summary>A handle that owns nothing.</summary>
    private sealed class Nothing : IDisposable
    {
        public static readonly Nothing Instance = new();

        public void Dispose()
        {
        }
    }
}
