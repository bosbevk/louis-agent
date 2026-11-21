namespace LouisAgent.Web.Components.Widget.Layouts;

/// <summary>
/// What the chat widget adds to its own classes for its layout (F164.2): the bar its own, the corner open or not as it
/// always was, and docked a panel in its page's place, beside the page, that slides in from the side below an
/// extra-large screen, with a button only there to open it.
/// </summary>
/// <remarks>
/// Ask the type (ADR 0123): what a layout adds is known here once, never worked out in the widget's markup, which
/// keeps each element's own class so its styles are seen to land on it. Docked is Bootstrap's responsive offcanvas,
/// <c>offcanvas-xl</c>, which stands in place from an extra-large screen and slides in below it, so no style of ours
/// writes a breakpoint (ADR 0085).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0085-styles-are-written-in-sass.md">ADR 0085: styles are written in Sass</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public static class WidgetLook
{
    /// <summary>What the widget adds for its layout, and whether it is open.</summary>
    public static string StateOf(WidgetLayout layout, bool open) => layout switch
    {
        WidgetLayout.Bar => "is-bar",
        WidgetLayout.Docked => open ? "is-docked is-open" : "is-docked",
        _ => open ? "is-open" : ""
    };

    /// <summary>
    /// What the open panel adds: docked, its place and Bootstrap's offcanvas; else the visitor's size.
    /// </summary>
    public static string PanelOf(WidgetLayout layout, ChatWindowSize size) => layout == WidgetLayout.Docked
        ? "is-docked offcanvas-xl offcanvas-end show"
        : size.ClassesOf("").Trim();

    /// <summary>What the round button adds: docked, it is there only where the page has no room beside it.</summary>
    public static string ButtonOf(WidgetLayout layout) => layout == WidgetLayout.Docked
        ? "d-xl-none position-fixed bottom-0 end-0 m-3"
        : "";

    /// <summary>Whether the panel offers minimise and maximise: docked, its size is its place's.</summary>
    public static bool HasWindowButtons(WidgetLayout layout) => layout != WidgetLayout.Docked;
}
