namespace LouisAgent.Core.Logging;

using Serilog.Core;
using Serilog.Events;

/// <summary>
/// Gives each event the <see cref="AgentLogging.TagProperty"/> the console prints in brackets: the level as the lines
/// have always read (<c>INFO</c>, <c>WARN</c>, <c>ERROR</c>), unless the call set its own tag (<c>MCP</c>, <c>TOOL</c>).
/// </summary>
internal sealed class LevelTagEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) =>
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(AgentLogging.TagProperty, logEvent.Level switch
        {
            LogEventLevel.Verbose => "TRACE",
            LogEventLevel.Debug => "DEBUG",
            LogEventLevel.Information => "INFO",
            LogEventLevel.Warning => "WARN",
            LogEventLevel.Error => "ERROR",
            _ => "FATAL",
        }));
}