using LouisAgent.Choices;

namespace LouisAgent.Packs.ShopPrices.Prices.Regions;

/// <summary>
/// Reads the province a model names, the way a person writes it ("Western Cape", "Wes-Kaap", "KZN"), into a
/// <see cref="Region"/>. Spaces count as dashes, so "Western Cape" is the wire name "western-cape".
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public static class RegionNames
{
    /// <summary>The Afrikaans names and the usual short forms, beside the wire names the enum already reads.</summary>
    private static readonly IReadOnlyDictionary<string, Region> Aliases = new Dictionary<string, Region>
    {
        ["south-africa"] = Region.National,
        ["suid-afrika"] = Region.National,
        ["sa"] = Region.National,
        ["wes-kaap"] = Region.WesternCape,
        ["oos-kaap"] = Region.EasternCape,
        ["noord-kaap"] = Region.NorthernCape,
        ["vrystaat"] = Region.FreeState,
        ["kzn"] = Region.KwaZuluNatal,
        ["kwa-zulu-natal"] = Region.KwaZuluNatal,
        ["kwazulu"] = Region.KwaZuluNatal,
        ["noordwes"] = Region.NorthWest,
        ["noord-wes"] = Region.NorthWest,
        ["gp"] = Region.Gauteng,
    };

    /// <summary>
    /// The region a name means. A name that is empty means the country; one that names nothing we know is
    /// refused, so the tool can say which names it takes.
    /// </summary>
    public static bool TryParse(string? name, out Region region)
    {
        string text = string.Join('-', (name ?? "").Trim().ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
        region = Region.National;
        if (text.Length == 0) return true;
        if (!SettingChoice.TryParse(text, out Region? parsed, Aliases) || parsed is null) return false;

        region = parsed.Value;
        return true;
    }
}
