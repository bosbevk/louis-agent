using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Brand;

/// <summary>
/// A flag beside a site's name that says what stage it is at, such as alpha-1, and opens a short note on a press (FX170,
/// Louis: "flag in the header version: alpha disclosure"). A site gives the words.
/// </summary>
/// <remarks>
/// The browser's own popover: the flag names its note, and the browser opens it, closes it on a press elsewhere or on
/// Escape, and stands it over the page. Chosen over a hover note, which a phone and a keyboard can't open, and over a
/// script of our own that would do what the browser already does.
/// </remarks>
/// <seealso href="https://developer.mozilla.org/docs/Web/API/Popover_API">The Popover API (MDN)</seealso>
public partial class StageFlag
{
    /// <summary>The note's id, which the flag names; one flag stands on a page.</summary>
    private const string NoteId = "stage-note";

    /// <summary>The flag's words, such as "alpha-1".</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = "";

    /// <summary>What the note says.</summary>
    [Parameter, EditorRequired]
    public string Note { get; set; } = "";

    /// <summary>The words of the link at the note's end.</summary>
    [Parameter, EditorRequired]
    public string LinkText { get; set; } = "";

    /// <summary>Where the link at the note's end goes.</summary>
    [Parameter, EditorRequired]
    public string Href { get; set; } = "";
}
