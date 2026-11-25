using LouisAgent.Choices;

namespace LouisAgent.Composition.Packs.Files;

/// <summary>
/// The kinds of file a model can read from a session (F171.1): four kinds of photo and a PDF, each by the media type
/// the wire names it with. A file of any other kind is refused before it is kept.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum FileMediaType
{
    /// <summary>A JPEG photo, as a phone takes one.</summary>
    [WireName("image/jpeg")] Jpeg,

    /// <summary>A PNG picture, as a screenshot is.</summary>
    [WireName("image/png")] Png,

    /// <summary>A GIF picture.</summary>
    [WireName("image/gif")] Gif,

    /// <summary>A WebP picture.</summary>
    [WireName("image/webp")] Webp,

    /// <summary>A PDF, as a municipality sends an account.</summary>
    [WireName("application/pdf")] Pdf
}
