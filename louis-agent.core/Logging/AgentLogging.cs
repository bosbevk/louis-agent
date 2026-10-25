namespace LouisAgent.Core.Logging;

using System.Globalization;
using System.Reflection;
using LouisAgent.Core.Logging.Messages;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Filters;
using SerilogConfiguration = Serilog.LoggerConfiguration;
using Serilog.Sinks.SystemConsole.Themes;

/// <summary>
/// Logging for every host: <see cref="ILogger"/> (Microsoft.Extensions.Logging) with Serilog behind it, configured once
/// per process by <c>AgentHost</c> (docs/CLAUDE.md, *Messages and logging*). Everything goes to <b>stderr</b>, never
/// stdout, which carries the ACP and MCP protocols. With a log directory, events marked as records (<see cref="RecordProperty"/>)
/// go to their own JSON-lines files instead of the console.
/// </summary>
public static class AgentLogging
{
    /// <summary>Console lines read as they always have: <c>[INFO] …</c>, <c>[ERROR] …</c>, <c>[MCP] …</c>.</summary>
    internal const string ConsoleTemplate = "[{Tag}] {Message:lj}{NewLine}{Exception}";

    /// <summary>The bracketed tag on a console line; <see cref="LevelTagEnricher"/> sets it from the level unless the call did.</summary>
    public const string TagProperty = "Tag";

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
    /// A run file's lines: the console's, with the time first, as the stderr copy per run always read. <c>:l</c>, not
    /// <c>:lj</c>: the file's formatter would write a non-string value (a Uri) as JSON, in quotes; the console's doesn't.
    /// </summary>
    internal const string FileTemplate = "{Timestamp:HH:mm:ss.fff} [{Tag}] {Message:l}{NewLine}{Exception}";

    // The run file per log directory: a second Configure with the same directory keeps writing to the same file.
    private static (string Directory, string File)? _runFile;

    /// <summary>The file this run logs to, or null without a log directory.</summary>
    public static string? RunFile => _runFile?.File;

    /// <summary>
    /// Console to stderr; with <paramref name="logDirectory"/>, a file per run (<c>agent-{host}-{time}-{process}.log</c>:
    /// containers run with --rm, so their own logs disappear on exit) and the record files too. Replaces (and flushes) the
    /// previous configuration, so it is safe to call again.
    /// </summary>
    public static void Configure(string? logDirectory)
    {
        var configuration = new SerilogConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .Enrich.With(new LevelTagEnricher())
            .WriteTo.Logger(console => console
                .Filter.ByExcluding(Matching.WithProperty(RecordProperty))
                .WriteTo.Console(outputTemplate: ConsoleTemplate, formatProvider: CultureInfo.InvariantCulture,
                    standardErrorFromLevel: LogEventLevel.Verbose, theme: ConsoleTheme.None));

        Exception? unusable = null;
        string? runFile = null;
        if (!string.IsNullOrWhiteSpace(logDirectory))
        {
            try
            {
                Directory.CreateDirectory(logDirectory);
                runFile = _runFile is { } previous && previous.Directory == logDirectory ? previous.File : NewRunFile(logDirectory);
                configuration.WriteTo.Logger(file => file
                    .Filter.ByExcluding(Matching.WithProperty(RecordProperty))
                    .WriteTo.File(runFile, outputTemplate: FileTemplate, formatProvider: CultureInfo.InvariantCulture, shared: true));
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
            log.Log(LogLevel.Warning, LoggingLogMessage.FileLoggingDisabled, new { Directory = logDirectory, Reason = unusable.Message });
        else if (announce)
            log.Log(LogLevel.Information, LoggingLogMessage.LoggingTo, new { Path = runFile });
    }

    private static string NewRunFile(string directory)
    {
        string host = (Assembly.GetEntryAssembly()?.GetName().Name ?? "agent").Replace("louis-agent.", "");
        return Path.Combine(directory, $"agent-{host}-{DateTime.Now:yyyyMMdd-HHmmss}-{Environment.ProcessId}.log");
    }
}