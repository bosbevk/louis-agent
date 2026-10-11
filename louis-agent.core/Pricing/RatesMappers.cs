namespace LouisAgent.Core.Pricing;

using System.Globalization;

/// <summary>The mapper for a <c>RATES_FORMAT</c> value.</summary>
public static class RatesMappers
{
    public static IRatesMapper For(string? format) => (format ?? FrankfurterRatesMapper.FormatName).ToLower(CultureInfo.InvariantCulture) switch
    {
        FrankfurterRatesMapper.FormatName => new FrankfurterRatesMapper(),
        RatesJsonMapper.FormatName => new RatesJsonMapper(),
        _ => throw new InvalidDataException(
            $"RATES_FORMAT must be '{FrankfurterRatesMapper.FormatName}' or '{RatesJsonMapper.FormatName}', got '{format}'."),
    };
}
