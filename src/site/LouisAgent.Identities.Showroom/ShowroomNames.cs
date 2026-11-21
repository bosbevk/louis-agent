using LouisAgent.Choices;

namespace LouisAgent.Identities.Showroom;

/// <summary>The showroom's businesses, by their names (ADR 0043).</summary>
public enum ShowroomIdentityName
{
    [WireName("wheatfield-kitchen")] WheatfieldKitchen,
    [WireName("koringland-attorneys")] KoringlandAttorneys,
    [WireName("seemeeu-guesthouse")] SeemeeuGuesthouse,
}

/// <summary>
/// The packs a showroom business names (ADR 0043: an identity keeps its own enum and never references a pack).
/// </summary>
public enum ShowroomPack
{
    Leads,
    Site,
}
