namespace LouisAgent.Core.Usage.Sinks;

using LouisAgent.Core.Usage.Records;
using System.Text;
using System.Text.Json;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.Logging;

/// <summary>
/// Appends each record as one JSON line to <c>{directory}/usage-YYYY-MM.jsonl</c> (monthly, append-only), and raises
/// <see cref="UsageRecorded"/> for hosts that show live totals (F3). A write that fails logs a warning and never reaches
/// the model call.
/// </summary>
/// <remarks>
/// Writes its own file rather than through <see cref="AgentLogging"/>: the ledger is data, not logs, its directory is
/// set per sink (a test points it at a temporary folder), and its field names are the usage spec's, not Serilog's.
/// </remarks>
public sealed class JsonlUsageSink(string directory) : IUsageSink
{
    private static readonly ILogger Log = AgentLogging.For<JsonlUsageSink>();

    // The usage spec's field names: duration_ms, cache_write, ... Nulls are written, since null means "not reported".
    internal static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private static readonly object Gate = new();

    /// <summary>Raised after each record is written, or after a write failed.</summary>
    public event Action<UsageRecord>? UsageRecorded;

    public UsageRecord Record(UsageRecord record)
    {
        string fileName = FileNameFor(record.At);
        try
        {
            byte[] line = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(record, Json) + "\n");
            lock (Gate)
            {
                Directory.CreateDirectory(directory);
                // Shared, so another process (the orchestrator next to the API) can append to the same month at once.
                using var file = new FileStream(Path.Combine(directory, fileName), FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                file.Write(line);
            }
        }
        catch (Exception ex)
        {
            Log.Log(LogLevel.Warning, LogMessage.LedgerWriteFailed, new { File = fileName, Reason = ex.Message });
        }

        UsageRecorded?.Invoke(record);
        // Returned even when the write failed: the request still happened, so the turn's totals still count it.
        return record;
    }

    /// <summary>One file per month, named by when the request finished (UTC), e.g. usage-2026-10.jsonl.</summary>
    internal static string FileNameFor(DateTimeOffset at) => $"usage-{at.UtcDateTime:yyyy-MM}.jsonl";
}