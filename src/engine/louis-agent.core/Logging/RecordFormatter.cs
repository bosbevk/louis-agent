namespace LouisAgent.Core.Logging;

using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

/// <summary>
/// A record's JSON line, in the shape these files always had (<c>{"timestamp":…,"tool":…,"chars":…}</c>): the event's
/// time and its fields, camelCase, and nothing of Serilog's own (<c>@mt</c>, <c>Tag</c>, <c>SourceContext</c>, the
/// <c>Record</c> marker). Readers of the files (F16's report) don't need to know the logger behind them.
/// </summary>
internal sealed class RecordFormatter : ITextFormatter
{
    private static readonly HashSet<string> Internal =
        [AgentLogging.RecordProperty, AgentLogging.TagProperty, "SourceContext"];

    private static readonly JsonValueFormatter Values = new(typeTagName: null);

    public void Format(LogEvent logEvent, TextWriter output)
    {
        output.Write("{\"timestamp\":\"");
        output.Write(logEvent.Timestamp.ToString("o"));
        output.Write('"');
        foreach (var (name, value) in logEvent.Properties)
        {
            if (Internal.Contains(name)) continue;
            output.Write(',');
            JsonValueFormatter.WriteQuotedJsonString(char.ToLowerInvariant(name[0]) + name[1..], output);
            output.Write(':');
            Values.Format(value, output);
        }
        output.WriteLine('}');
    }
}