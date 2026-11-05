using LouisAgent.Composition.Logging.Content;
using LouisAgent.Composition.Logging;
using System.Globalization;
using System.Reflection;
using LouisAgent.Core.Logging.Messages;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Filters;
using SerilogConfiguration = Serilog.LoggerConfiguration;
using Serilog.Sinks.SystemConsole.Themes;

namespace LouisAgent.Core.Logging;

/// <summary>
/// Logging for every host: <see cref="Microsoft.Extensions.Logging.ILogger"/> with Serilog behind it, configured once
/// per process by <c>AgentHost</c> (src/docs/CLAUDE.md, *Messages and logging*). Everything goes to <b>stderr</b>, never
/// stdout, which carries the ACP and MCP protocols. With a log directory, events marked as records
/// (<see cref="RecordProperty"/>) go to their own JSON-lines files instead of the console. With
/// <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> set, every line also goes to that OpenTelemetry collector, named for its host
/// (F52, ADR 0064).
/// </summary>
public static class AgentLogging
{
    /// <summary>Console lines read as they always have: <c>[INFO] …</c>, <c>[ERROR] …</c>, <c>[MCP] …</c>.</summary>
    internal const string ConsoleTemplate = "[{Tag}] {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// The bracketed tag on a console line; <see cref="LevelTagEnricher"/> sets it from the level unless the call did.
    /// </summary>
    public const string TagProperty = MessageLogging.TagProperty;

    /// <summary>
    /// The property that marks an event as a record for a JSON-lines file, its value the record's kind.
    /// </summary>
    public const string RecordProperty = "Record";

    /// <summary>Each record kind and its file under the log directory.</summary>
    internal static readonly IReadOnlyDictionary<string, string> RecordFiles = new Dictionary<string, string>
    {
        ["oversized-tool-result"] = "oversized-tool-results.jsonl",
        ["truncated-tool-call"] = "truncated-tool-calls.jsonl",
    };

    /// <summary>
    /// The root logger's one sink, forwarding to the current configuration: loggers bind to the root when created, so
    /// one made before <see cref="Configure"/> (a static field) still writes where Configure says.
    /// </summary>
    private static readonly ForwardingSink Current = new();

    /// <summary>The one root logger for the life of the process.</summary>
    private static readonly Serilog.Core.Logger Root =
        new SerilogConfiguration().MinimumLevel.Verbose().WriteTo.Sink(Current).CreateLogger();

    /// <summary>Hands out <see cref="Microsoft.Extensions.Logging.ILogger"/>s over the root.</summary>
    private static readonly SerilogLoggerFactory Factory = new(Root);

    /// <summary>The console from the start, before any host has configured logging.</summary>
    static AgentLogging()
    {
        Configure(null);
        PackLog.Use(Factory);
    }

    /// <summary>The factory a host gives to whatever wants one (ASP.NET Core, the MCP SDK).</summary>
    public static ILoggerFactory LoggerFactory => Factory;

    /// <summary>A logger named for the class that logs.</summary>
    public static ILogger<T> For<T>() => new Logger<T>(Factory);

    /// <summary>
    /// A run file's lines: the console's, with the time first, as the stderr copy per run always read. <c>:l</c>, not
    /// <c>:lj</c>: the file's formatter would write a non-string value (a Uri) as JSON, in quotes; the console's
    /// doesn't.
    /// </summary>
    internal const string FileTemplate = "{Timestamp:HH:mm:ss.fff} [{Tag}] {Message:l}{NewLine}{Exception}";

    /// <summary>
    /// The run file per log directory: a second <see cref="Configure"/> with the same directory keeps writing to it.
    /// </summary>
    private static (string Directory, string File)? _runFile;

    /// <summary>What the last <see cref="Configure"/> was given, so <see cref="Flush"/> can make it again.</summary>
    private static (string? Directory, LogContentMode Content) _configured;

    /// <summary>
    /// The standard setting that names an OTLP collector (F52); unset, no log line or span leaves the process.
    /// </summary>
    internal const string OtlpEndpointVariable = "OTEL_EXPORTER_OTLP_ENDPOINT";

    /// <summary>The file this run logs to, or null without a log directory.</summary>
    public static string? RunFile => _runFile?.File;

    /// <summary>
    /// Console to stderr; with <paramref name="logDirectory"/>, a file per run
    /// (<c>agent-{host}-{time}-{process}.log</c>: containers run with --rm, so their own logs disappear on exit) and
    /// the record files too; with an OTLP endpoint set, the collector. Replaces (and flushes) the previous
    /// configuration, so it is safe to call again.
    /// </summary>
    public static void Configure(string? logDirectory, LogContentMode content = LogContentMode.Hashed)
    {
        _configured = (logDirectory, content);
        LogContent.Use(content);
        var configuration = new SerilogConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .Enrich.With(new LevelTagEnricher())
            .WriteTo.Logger(console => console
                .Filter.ByExcluding(Matching.WithProperty(RecordProperty))
                .WriteTo.Console(outputTemplate: ConsoleTemplate, formatProvider: CultureInfo.InvariantCulture,
                    standardErrorFromLevel: LogEventLevel.Verbose, theme: ConsoleTheme.None));

        // With a collector named the standard way, every line goes there too, the records left out as on the console.
        // The sink reads the endpoint, protocol, headers and OTEL_SERVICE_NAME itself, and they win over ours.
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(OtlpEndpointVariable)))
            configuration.WriteTo.Logger(otlp => otlp
                .Filter.ByExcluding(Matching.WithProperty(RecordProperty))
                .WriteTo.OpenTelemetry(options => options.ResourceAttributes["service.name"] = ServiceName));

        Exception? unusable = null;
        string? runFile = null;
        if (!string.IsNullOrWhiteSpace(logDirectory))
        {
            try
            {
                Directory.CreateDirectory(logDirectory);
                runFile = _runFile is { } previous && previous.Directory == logDirectory
                    ? previous.File
                    : NewRunFile(logDirectory);
                configuration.WriteTo.Logger(file => file
                    .Filter.ByExcluding(Matching.WithProperty(RecordProperty))
                    .WriteTo.File(runFile, outputTemplate: FileTemplate, formatProvider: CultureInfo.InvariantCulture,
                        shared: true));
                foreach (var (record, file) in RecordFiles)
                {
                    configuration.WriteTo.Logger(records => records
                        .Filter.ByIncludingOnly(Matching.WithProperty<string>(RecordProperty, kind => kind == record))
                        .WriteTo.File(new RecordFormatter(), Path.Combine(logDirectory, file), shared: true));
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                unusable = ex;
                runFile = null;
            }
        }

        bool announce = runFile is not null && _runFile?.File != runFile;
        _runFile = runFile is null ? null : (logDirectory!, runFile);
        Current.Replace(configuration.CreateLogger());

        var log = Factory.CreateLogger(typeof(AgentLogging));
        if (unusable is not null)
            log.Log(LogLevel.Warning, LoggingLogMessage.FileLoggingDisabled,
                new
                {
                    Directory = logDirectory,
                    Reason = unusable.Message
                });
        else if (announce)
            log.Log(LogLevel.Information, LoggingLogMessage.LoggingTo, new { Path = runFile });
    }

    /// <summary>
    /// Sends what the sinks still hold, the collector's batch above all, and goes on logging as before. A configuration
    /// builds its logger once, so the same configuration is made again: replacing the logger disposes the old one,
    /// which is what makes a batching sink send.
    /// </summary>
    internal static void Flush() => Configure(_configured.Directory, _configured.Content);

    /// <summary>The host's name as the logs give it: the entry assembly's name without <c>louis-agent.</c>.</summary>
    internal static string ServiceName =>
        (Assembly.GetEntryAssembly()?.GetName().Name ?? "agent").Replace("louis-agent.", "");

    /// <summary>A new run file's path in the directory, named for the host, the time and the process.</summary>
    private static string NewRunFile(string directory) =>
        Path.Combine(directory, $"agent-{ServiceName}-{DateTime.Now:yyyyMMdd-HHmmss}-{Environment.ProcessId}.log");
}
