using System.ComponentModel;

namespace LouisAgent.Core.Telemetry.Messages;

/// <summary>
/// The process's last word (F52 step 5): a crash and a lost task. No holes: the exception itself goes with the line,
/// printed under it on the console and sent as fields to the collector.
/// </summary>
public enum TelemetryLogMessage
{
    [Description("The process is ending on an exception nothing caught")]
    UnhandledException,

    [Description("A task failed and nothing awaited it")]
    UnobservedTaskException,
}
