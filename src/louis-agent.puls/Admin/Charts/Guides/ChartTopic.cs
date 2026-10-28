namespace LouisAgent.Puls.Admin.Charts.Guides;

/// <summary>
/// The measures a chart can show, shared by the explorer's rings and the trend's lines. A page names one by its word
/// (<c>cost</c>, <c>passat</c>), which is the member's own name in any case.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum ChartTopic
{
    Cost,
    Tokens,
    Time,
    Routing,
    Accuracy,
    Cache,
    Tools,
    Sources,
    PassAt,
    PassHat,
    Value
}
