namespace louis_agent.core.usage;

using System.Text.Json;

/// <summary>Totals of a group of usage records: tokens by kind, requests, and cost.</summary>
/// <param name="Cost">The sum of the priced records; null when none of them has a cost.</param>
/// <param name="Unpriced">Records without a cost ("price unknown"); their tokens are in the totals, their cost isn't.</param>
public sealed record UsageTotal(
    string Key, int Requests, long Input, long CacheWrite, long CacheRead, long Output, decimal? Cost, int Unpriced);

/// <summary>Reads a usage ledger back, prices records written before F2, and totals them (the M1 baseline; F3 and F11 later).</summary>
public static class UsageReport
{
    /// <summary>The records in a ledger file, one per JSON line; a line that isn't a record throws naming its number.</summary>
    public static IEnumerable<UsageRecord> Read(string path)
    {
        int number = 0;
        foreach (string line in File.ReadLines(path))
        {
            number++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            UsageRecord? record;
            try
            {
                record = JsonSerializer.Deserialize<UsageRecord>(line, JsonlUsageSink.Json);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"{path} line {number} is not a usage record: {ex.Message}", ex);
            }

            if (record is not null) yield return record;
        }
    }

    /// <summary>
    /// Records without a cost priced from <paramref name="prices"/>; records that have one keep it, since a stored cost
    /// is what the request was charged at.
    /// </summary>
    public static IEnumerable<UsageRecord> Price(IEnumerable<UsageRecord> records, PriceTable prices) =>
        records.Select(r => r.Cost is null ? r with { Cost = CostCalculator.For(r, prices) } : r);

    /// <summary>One total per key, in the order the keys first appear.</summary>
    public static IReadOnlyList<UsageTotal> Totals(IEnumerable<UsageRecord> records, Func<UsageRecord, string> keyOf) =>
        records.GroupBy(keyOf).Select(group => Total(group.Key, group)).ToList();

    /// <summary>The total of all <paramref name="records"/>.</summary>
    public static UsageTotal Total(string key, IEnumerable<UsageRecord> records)
    {
        var list = records.ToList();
        var priced = list.Where(r => r.Cost is not null).ToList();
        return new UsageTotal(
            key,
            list.Count,
            list.Sum(r => r.Tokens.Input ?? 0),
            list.Sum(r => r.Tokens.CacheWrite ?? 0),
            list.Sum(r => r.Tokens.CacheRead ?? 0),
            list.Sum(r => r.Tokens.Output ?? 0),
            priced.Count == 0 ? null : priced.Sum(r => r.Cost!.Amount),
            list.Count - priced.Count);
    }

    /// <summary>
    /// The piece of work a record belongs to: its task (e.g. <c>fix:e8b0b572223d</c>), else its triage or chat session.
    /// </summary>
    public static string WorkOf(UsageRecord record) =>
        record.Task ?? (record.Purpose == UsagePurpose.Triage ? $"triage:{record.Session}" : record.Session ?? "(no session)");
}