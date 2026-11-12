using System.Text.Json;

namespace LouisAgent.Api.Sessions.Log;

/// <summary>
/// A line per finished turn, beside the ledger (F127.8): one JSON line in <c>turns-{yyyy-MM}.jsonl</c> in the log
/// folder, which the admin's overview reads back. With no folder it writes nothing and reads nothing.
/// </summary>
/// <remarks>
/// A file of its own, as the ledger is (<c>JsonlUsageSink</c>): the ledger counts requests and their cost, and this
/// counts turns and where they came from, so neither record carries the other's fields, and the overview joins the two
/// by session. JSON through a record and one options object for this file (ADR 0041).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed class TurnLog(string? folder)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly Lock Writing = new();

    /// <summary>Adds the turn's line to its month's file.</summary>
    public void Write(TurnLogRecord record)
    {
        if (folder is null) return;
        string line = JsonSerializer.Serialize(record, Json) + "\n";
        // Turns of several sessions end at once: one line at a time, so no two lines run into each other.
        lock (Writing)
        {
            Directory.CreateDirectory(folder);
            File.AppendAllText(PathOf(DateOnly.FromDateTime(record.At.UtcDateTime)), line);
        }
    }

    /// <summary>The month's lines, in the order they were written; none without a folder or a file.</summary>
    public IEnumerable<TurnLogRecord> Read(DateOnly month)
    {
        if (folder is null || !File.Exists(PathOf(month))) return [];
        return File.ReadLines(PathOf(month)).Where(line => line.Length > 0)
            .Select(line => JsonSerializer.Deserialize<TurnLogRecord>(line, Json)!).ToList();
    }

    /// <summary>The file of the month a day falls in.</summary>
    private string PathOf(DateOnly day) => Path.Combine(folder!, $"turns-{day:yyyy-MM}.jsonl");
}
