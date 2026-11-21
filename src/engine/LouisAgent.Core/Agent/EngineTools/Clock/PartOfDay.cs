using LouisAgent.Choices;

namespace LouisAgent.Core.Agent.EngineTools.Clock;

/// <summary>
/// The part of the day where the user is (FX127), so a greeting or "this evening" follows the clock rather than the
/// model's reading of an hour: morning from 05:00, afternoon from 12:00, evening from 18:00, night from 22:00.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0080-a-tool-when-it-improves-the-prompt.md">ADR 0080: a tool when it improves the prompt</seealso>
public enum PartOfDay
{
    [WireName("morning")] Morning,
    [WireName("afternoon")] Afternoon,
    [WireName("evening")] Evening,
    [WireName("night")] Night,
}
