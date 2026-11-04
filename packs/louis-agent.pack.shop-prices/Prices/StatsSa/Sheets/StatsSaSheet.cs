using System.ComponentModel;
using LouisAgent.Choices;

namespace LouisAgent.Packs.ShopPrices.Prices.StatsSa.Sheets;

/// <summary>
/// Stats SA's two average-price spreadsheets (P0141): all urban areas, the country's average, and the nine provinces.
/// The wire name is the upload's file slot; the description is how a problem names the file.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum StatsSaSheet
{
    [WireName("all-urban")][Description("all urban areas")] AllUrban,
    [WireName("provinces")][Description("provinces")] Provinces,
}
