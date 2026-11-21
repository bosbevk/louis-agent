using LouisAgent.Choices;

namespace LouisAgent.Packs.Triage.Tools.Choices;

/// <summary>Whose area an error's cause is in: the code's, or another department's, which gets the case.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum DepartmentArea
{
    [WireName("code")] Code,
    [WireName("data")] Data,
    [WireName("hosting")] Hosting,
    [WireName("third-party")] ThirdParty,
    [WireName("unknown")] Unknown,
}
