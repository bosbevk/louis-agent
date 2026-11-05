namespace LouisAgent.Composition.Pricing;

/// <summary>The day's exchange rates, for a tool that converts money.</summary>
/// <remarks>Separated Interface: the engine loads the rates once, for its own cost display and for any pack.</remarks>
public interface ICurrencyRates
{
    /// <summary>When there are no rates at all.</summary>
    bool IsEmpty { get; }

    /// <summary>The currency the rates are against.</summary>
    string Base { get; }

    /// <summary>The day they're from.</summary>
    string AsOf { get; }

    /// <summary>Where they came from, when known.</summary>
    string? Source { get; }

    /// <summary>Each currency's rate against the base.</summary>
    IReadOnlyDictionary<string, decimal> Rates { get; }

    /// <summary>One currency's rate, or nothing when it's unknown.</summary>
    decimal? RateFor(string currency);
}
