using LouisAgent.Web.Usage;

namespace LouisAgent.Web.Components.Admin.Overview;

/// <summary>
/// A cost on the admin's overview (F127.8): in the reader's currency, converted on the screen as every cost the web
/// shows is (F14), and "unknown" when none of it was priced (ADR 0007).
/// </summary>
public static class OverviewMoney
{
    /// <summary>What an unpriced cost says: unknown, never 0.</summary>
    public const string Unknown = "unknown";

    /// <summary>The cost written for the reader, or unknown.</summary>
    public static string Of(decimal? cost, string currency, CurrencyChoice? choice) =>
        cost is { } amount ? UsageFormat.Money(amount, currency, choice) : Unknown;
}
