using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Actions;

/// <summary>
/// A short line under a hero that tells visitors about something new and takes them to its own page, such as an
/// offer. The whole line is one link, so it is easy to tap on a phone.
/// </summary>
/// <remarks>
/// Composite: it puts the framework's own status pill and a sentence into one link, so a site only gives its words
/// and an address (ADR 0125), and every site's announcement looks the same in every theme.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class AnnouncementStrip
{
    /// <summary>The page the line leads to.</summary>
    [Parameter, EditorRequired] public string Href { get; set; } = "";

    /// <summary>What the line announces, in one sentence.</summary>
    [Parameter, EditorRequired] public string Text { get; set; } = "";

    /// <summary>The words at the end that say where it goes.</summary>
    [Parameter, EditorRequired] public string LinkText { get; set; } = "";

    /// <summary>A status shown first in a pill, such as "Entries open"; none by default.</summary>
    [Parameter] public string? Status { get; set; }
}
