namespace LouisAgent.Core.Usage;

using LouisAgent.Core.Pricing;

/// <summary>Where usage records go. The JSONL ledger in production; a list in tests.</summary>
public interface IUsageSink
{
    /// <summary>
    /// Stores one record and returns it as stored, e.g. with its cost added (<see cref="PricingUsageSink"/>), so the turn's
    /// <see cref="UsageScope"/> totals what the ledger has. Must never throw into the model call: a broken ledger only logs
    /// a warning.
    /// </summary>
    UsageRecord Record(UsageRecord record);
}