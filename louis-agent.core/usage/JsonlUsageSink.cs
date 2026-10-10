namespace louis_agent.core.usage;

using System.Text;
using System.Text.Json;

/// <summary>
/// Appends each record as one JSON line to <c>{directory}/usage-YYYY-MM.jsonl</c> (monthly, append-only), and raises
/// <see cref="UsageRecorded"/> for hosts that show live totals (F3). A write that fails logs a warning and never reaches
/// the model call.
/// </summary>
/// <remarks>
/// Writes its own file rather than through <see cref="AgentLog"/>: AgentLog's directory is set once per process, which a
/// test can't point at a temporary folder.
/// </remarks>
public sealed class JsonlUsageSink(string directory) : IUsageSink
{
    // The usage spec's field names: duration_ms, cache_write, ... Nulls are written, since null means "not reported".
    internal static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private static readonly object Gate = new();

    /// <summary>Raised after each record is written, or after a write failed.</summary>
    public event Action<UsageRecord>? UsageRecorded;

    public void Record(UsageRecord record)
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
            Console.Error.WriteLine($"[WARN] Could not write {fileName}: {ex.Message}");
        }

        UsageRecorded?.Invoke(record);
    }

    /// <summary>One file per month, named by when the request finished (UTC), e.g. usage-2026-10.jsonl.</summary>
    internal static string FileNameFor(DateTimeOffset at) => $"usage-{at.UtcDateTime:yyyy-MM}.jsonl";
}