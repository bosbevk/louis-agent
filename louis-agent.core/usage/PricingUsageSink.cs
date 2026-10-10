namespace louis_agent.core.usage;

/// <summary>
/// Puts the cost on each record (<see cref="CostCalculator"/>), then hands it to the inner sink. Pricing happens here,
/// where every host's records already pass (<c>AgentHost.CreateUsageSink</c>), so the engine and the hosts don't change,
/// and the inner sink's <c>UsageRecorded</c> event sees the cost too.
/// </summary>
public sealed class PricingUsageSink(IUsageSink inner, PriceTable prices) : IUsageSink
{
    /// <summary>The sink the priced records go to, e.g. the JSONL ledger with its <c>UsageRecorded</c> event.</summary>
    public IUsageSink Inner => inner;

    public void Record(UsageRecord record) =>
        // A record that already has a cost keeps it: a cost is stored with the prices it was charged at.
        inner.Record(record.Cost is null ? record with { Cost = CostCalculator.For(record, prices) } : record);
}