using LouisAgent.Choices;

namespace LouisAgent.Diagrams.Engine.Svg.Fits;

/// <summary>
/// Which screen a page's drawing is for (F169.5): a wide one, as every picture is drawn today, or a phone's narrow
/// one, where a picture that has a narrow form is drawn in it.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum PictureFit
{
    [WireName("wide")] Wide,
    [WireName("narrow")] Narrow
}
