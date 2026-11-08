using LouisAgent.Choices;

namespace LouisAgent.Packs.Leads;

/// <summary>This pack's one toolset, by its wire name (ADR 0043: a repeated name is an enum, one copy per pack).</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public enum LeadsToolset
{
    [WireName("leads")] Leads,
}
