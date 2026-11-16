using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Controls.Inputs;

/// <summary>A switch a visitor turns on and off: the theme draws it on, off, and off-and-unusable.</summary>
/// <remarks>
/// The design page's switch (FX162, Louis: "the design doc is the truth here"), configured by a site with what it
/// says to a screen reader and whether it starts on (ADR 0125).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class Switch
{
    /// <summary>What it turns on, for a screen reader.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = "";

    /// <summary>Whether it is on.</summary>
    [Parameter] public bool On { get; set; }

    /// <summary>Tells the page when it turns.</summary>
    [Parameter] public EventCallback<bool> OnChanged { get; set; }

    /// <summary>Shown but not to be used.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Turns it over and says so.</summary>
    private async Task Toggle()
    {
        On = !On;
        await OnChanged.InvokeAsync(On);
    }
}
