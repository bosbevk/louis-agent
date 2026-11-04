using LouisAgent.Choices;

namespace LouisAgent.Puls.Admin.RunPage;

/// <summary>The run page's tabs; the wire name is what the address carries in <c>?tab=</c>.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum RunTab
{
    [WireName("summary")] Summary,
    [WireName("conversations")] Conversations,
    [WireName("judge")] Judge,
    [WireName("notes")] Notes,
    [WireName("settings")] Settings,
}
