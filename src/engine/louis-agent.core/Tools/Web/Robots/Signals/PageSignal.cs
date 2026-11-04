using LouisAgent.Choices;

namespace LouisAgent.Core.Tools.Web.Robots.Signals;

/// <summary>
/// The four ways a page can tell us on its own that it doesn't want AI readers: each says which one it was, in words
/// the refusal shows the model.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0068-every-fetch-obeys-robots-txt.md">ADR 0068: every fetch obeys robots.txt</seealso>
public enum PageSignal
{
    /// <summary>A <c>&lt;meta name="robots"&gt;</c> tag whose content has <c>noai</c>.</summary>
    [WireName("meta robots noai")] MetaNoAi,

    /// <summary>An <c>X-Robots-Tag</c> response header with <c>noai</c>.</summary>
    [WireName("X-Robots-Tag noai")] XRobotsTagNoAi,

    /// <summary>A <c>Content-Signal</c> response header with <c>ai-input=no</c>.</summary>
    [WireName("Content-Signal ai-input=no")]
    ContentSignalAiInputNo,

    /// <summary>A <c>tdm-reservation: 1</c> response header, the text-and-data-mining opt-out.</summary>
    [WireName("tdm-reservation 1")] TdmReservation,
}
