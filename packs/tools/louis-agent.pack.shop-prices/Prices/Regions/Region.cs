using System.ComponentModel;
using LouisAgent.Choices;

namespace LouisAgent.Packs.ShopPrices.Prices.Regions;

/// <summary>
/// Where an average price is for: the country, or one of the nine provinces Stats SA prices separately. The wire
/// name is the key in the price table; the description is how an answer names it.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum Region
{
    [WireName("national")][Description("South Africa")] National,
    [WireName("western-cape")][Description("the Western Cape")] WesternCape,
    [WireName("eastern-cape")][Description("the Eastern Cape")] EasternCape,
    [WireName("northern-cape")][Description("the Northern Cape")] NorthernCape,
    [WireName("free-state")][Description("the Free State")] FreeState,
    [WireName("kwazulu-natal")][Description("KwaZulu-Natal")] KwaZuluNatal,
    [WireName("north-west")][Description("North West")] NorthWest,
    [WireName("gauteng")][Description("Gauteng")] Gauteng,
    [WireName("mpumalanga")][Description("Mpumalanga")] Mpumalanga,
    [WireName("limpopo")][Description("Limpopo")] Limpopo,
}
