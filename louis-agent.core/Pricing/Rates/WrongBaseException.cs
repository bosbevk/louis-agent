namespace LouisAgent.Core.Pricing.Rates;

/// <summary>
/// Rates from another currency than the price table's. From RATES_FILE, our own configuration, it stops start-up; from
/// RATES_URL it falls back to the file, since a provider changing its reply is not something the operator can fix.
/// </summary>
internal sealed class WrongBaseException(string message) : Exception(message);
