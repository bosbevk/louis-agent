namespace LouisAgent.Web.Shared.Usage;

/// <summary>
/// The currency this browser shows costs in (F14): costs are stored as charged (in <see cref="Base"/>) and converted at
/// <see cref="Rate"/> only when shown, so changing it re-labels every saved chat.
/// </summary>
public sealed record CurrencyChoice(string Code, string Symbol, decimal Rate, string? AsOf, string Base)
{
    public bool Converts => Code != Base;
}
