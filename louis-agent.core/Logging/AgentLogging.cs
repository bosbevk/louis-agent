namespace LouisAgent.Core.Logging;

using System.Globalization;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Filters;
using Serilog.Formatting.Compact;
using Serilog.Sinks.SystemConsole.Themes;
using SerilogConfiguration = Serilog.LoggerConfiguration;

/// <summary>
/// Logging for every host: <see cref="ILogger"/> (Microsoft.Extensions.Logging) with Serilog behind it, configured once
/// per process by <c>AgentHost</c> (docs/CLAUDE.md, *Messages and logging*). Everything goes to <b>stderr</b>, never
/// stdout, which carries the ACP and MCP protocols. With a log directory, events marked as records (<see cref="RecordProperty"/>)
/// go to their own JSON-lines files instead of the console.
/// </summary>
public static class AgentLogging
{
    /// <summary>Console lines read as they always have: <c>[INFO] …</c>, <c>[WARN] …</c>.</summary>
    internal const string ConsoleTemplate = "[{Level:u4}] {Message:lj}{NewLine}{Exception}";

    /// <summary>The property that marks an event as a record for a JSON-lines file, its value the record's kind.</summary>
    public const string RecordProperty = "Record";

    /// <summary>Each record kind and its file under the log directory.</summary>
    internal static readonly IReadOnlyDictionary<string, string> RecordFiles = new Dictionary<string, string>
    {
        ["oversized-tool-result"] = "oversized-tool-results.jsonl",
        ["truncated-tool-call"] = "truncated-tool-calls.jsonl",
    };

    // One root logger for the life of the process, its only sink forwarding to the current configuration: loggers bind
    // to the root when created, so one made before Configure (a static field) still writes where Configure says.
    private static readonly ForwardingSink Current = new();
    private static readonly Serilog.Core.Logger Root =
        new SerilogConfiguration().MinimumLevel.Verbose().WriteTo.Sink(Current).CreateLogger();
    private static readonly SerilogLoggerFactory Factory = new(Root);

    static AgentLogging() => Configure(null);

    public static ILoggerFactory LoggerFactory => Factory;

    public static ILogger<T> For<T>() => new Logger<T>(Factory);

    /// <summary>
    /// Console to stderr; with <paramref name="logDirectory"/>, the record files too. Replaces (and flushes) the previous
    /// configuration, so it is safe to call again.
    /// </summary>
    public static void Configure(string? logDirectory)
    {
        var configuration = new SerilogConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .WriteTo.Logger(console => console
                .Filter.ByExcluding(Matching.WithProperty(RecordProperty))
                .WriteTo.Console(outputTemplate: ConsoleTemplate, formatProvider: CultureInfo.InvariantCulture,
                    standardErrorFromLevel: LogEventLevel.Verbose, theme: ConsoleTheme.None));

        if (!string.IsNullOrWhiteSpace(logDirectory))
        {
            foreach (var (record, file) in RecordFiles)
            {
                configuration.WriteTo.Logger(records => records
                    .Filter.ByIncludingOnly(Matching.WithProperty<string>(RecordProperty, kind => kind == record))
                    .WriteTo.File(new CompactJsonFormatter(), Path.Combine(logDirectory, file)));
            }
        }

        Current.Replace(configuration.CreateLogger());
    }
}