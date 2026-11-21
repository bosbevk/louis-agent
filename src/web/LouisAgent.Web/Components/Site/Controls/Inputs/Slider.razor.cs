using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Controls.Inputs;

/// <summary>A slider from 0 to 100 a visitor drags or steps with the keys; the theme draws its groove, fill and knob.</summary>
/// <remarks>
/// The design page's slider (FX162). A real range input sits over the drawings, so the pointer and the keyboard work
/// as they do on any slider, with no script (ADR 0125).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class Slider
{
    /// <summary>What it sets, for a screen reader.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = "";

    /// <summary>Where it stands, from 0 to 100.</summary>
    [Parameter] public int Value { get; set; } = 55;

    /// <summary>Tells the page where it moved to.</summary>
    [Parameter] public EventCallback<int> OnChanged { get; set; }

    /// <summary>Where the fill ends and the knob stands, as a share of the groove.</summary>
    private string Percent => Value.ToString(CultureInfo.InvariantCulture) + "%";

    /// <summary>Follows the input as it moves.</summary>
    private async Task Slide(ChangeEventArgs moved)
    {
        if (!int.TryParse(moved.Value?.ToString(), CultureInfo.InvariantCulture, out int at)) return;
        Value = at;
        await OnChanged.InvokeAsync(at);
    }
}
