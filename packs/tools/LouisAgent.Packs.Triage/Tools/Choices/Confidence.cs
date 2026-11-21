using LouisAgent.Choices;

namespace LouisAgent.Packs.Triage.Tools.Choices;

/// <summary>How sure the investigator is; the manager's rule in code reads it before it lets a fix run.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum Confidence
{
    [WireName("low")] Low,
    [WireName("medium")] Medium,
    [WireName("high")] High,
}
