using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Reports;

namespace LouisAgent.Api.Sessions.Streaming;

/// <summary>
/// What a turn (or a session) used: the total of its requests and the currency it was charged in. Computed once from the
/// ledger; a format renders it, converting the cost for display when the host shows another currency (F14).
/// </summary>
public sealed record TurnUsage(UsageTotal Total, string? Currency, DisplayCurrency? Display)
{
    public static TurnUsage From(IReadOnlyList<UsageRecord> records, DisplayCurrency? display)
    {
        UsageTotal total = UsageReport.Total("usage", records);
        string? currency = total.Cost is null ? null : records.First(r => r.Cost is not null).Cost!.Currency;
        return new TurnUsage(total, currency, display);
    }
}
