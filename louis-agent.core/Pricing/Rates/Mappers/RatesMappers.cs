
using System.Globalization;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Pricing.Messages;

namespace LouisAgent.Core.Pricing.Rates.Mappers;
/// <summary>The mapper for a <c>RATES_FORMAT</c> value.</summary>
public static class RatesMappers
{
    public static IRatesMapper For(string? format) =>
        (format ?? FrankfurterRatesMapper.FormatName).ToLower(CultureInfo.InvariantCulture) switch
        {
            FrankfurterRatesMapper.FormatName => new FrankfurterRatesMapper(),
            RatesJsonMapper.FormatName => new RatesJsonMapper(),
            _ => throw new InvalidDataException(
                MessageText.Format(PricingErrorMessage.RatesFormatUnknown,
                    new
                    {
                        Frankfurter = FrankfurterRatesMapper.FormatName,
                        Json = RatesJsonMapper.FormatName,
                        Format = format
                    })),
        };
}
