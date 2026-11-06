using System.Collections.Concurrent;
using LouisAgent.Composition.Logging;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Telemetry.Messages;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Core.Telemetry;

/// <summary>
/// The process's last word: an exception nothing caught is logged once, Critical, as the exception itself (type,
/// message and stack), and the logs and spans are sent before the process ends, so the crash reaches the dashboard.
/// A failed task nobody awaited is logged as an Error and marked seen: the process goes on (Louis's pick I1).
/// </summary>
/// <remarks>
/// A last-chance (catch-all) exception handler at the process boundary, on the runtime's two hooks for it,
/// <see cref="AppDomain.UnhandledException"/> and <see cref="TaskScheduler.UnobservedTaskException"/>, as
/// Microsoft's guidance places one. The rule it holds: log an exception once, where it is handled; a host that
/// catches its own main task logs there, and this never sees it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0064-observability-through-opentelemetry.md">ADR 0064: observability through OpenTelemetry</seealso>
internal static class LastChance
{
    /// <summary>Guards the subscription, so a second start doesn't log a crash twice.</summary>
    private static readonly Lock Gate = new();

    /// <summary>Whether this process is subscribed already.</summary>
    private static bool _installed;

    /// <summary>What its users said to expect: added at their start, read on the finalizer thread.</summary>
    private static readonly ConcurrentBag<Func<Exception, bool>> Expected = [];

    /// <summary>The handler's own lines.</summary>
    private static readonly ILogger Log = AgentLogging.LoggerFactory.CreateLogger(typeof(LastChance));

    /// <summary>Subscribes to the runtime's two events, once per process.</summary>
    public static void Install()
    {
        lock (Gate)
        {
            if (_installed) return;
            _installed = true;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandled;
            TaskScheduler.UnobservedTaskException += OnUnobserved;
        }
    }

    /// <summary>A failure a library leaves lost by design, that its user knows costs nothing: not worth an Error.</summary>
    public static void Expect(Func<Exception, bool> isExpected) => Expected.Add(isExpected);

    /// <summary>Logs the crash once, then sends the logs and the spans: the line first, the spans after.</summary>
    public static void Handle(Exception exception)
    {
        Log.Log(LogLevel.Critical, TelemetryLogMessage.UnhandledException, exception: exception);
        AgentLogging.Flush();
        AgentTelemetry.Flush();
    }

    /// <summary>
    /// Marks the lost task seen and logs it as an Error, or as Debug when all it holds was expected. No flush: this runs
    /// on the finalizer thread, and the process goes on, so the line leaves with the next batch.
    /// </summary>
    public static void OnUnobserved(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
        bool expected = e.Exception.InnerExceptions.All(failure => Expected.Any(isExpected => isExpected(failure)));
        Log.Log(expected ? LogLevel.Debug : LogLevel.Error, TelemetryLogMessage.UnobservedTaskException,
            exception: e.Exception);
    }

    /// <summary>The runtime's last call before the process ends on an exception nothing caught.</summary>
    private static void OnUnhandled(object sender, UnhandledExceptionEventArgs e)
    {
        // C# only ever throws exceptions; the event's object type is for other languages.
        if (e.ExceptionObject is Exception exception) Handle(exception);
    }
}
