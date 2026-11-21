namespace LouisAgent.Web.Components.Widget.Bar.Opening;

/// <summary>How a closed agent bar opens its box (FX172): a site picks one, and a press on the orb works with either.</summary>
public enum BarOpens
{
    /// <summary>On a press of the orb, the framework's default.</summary>
    Click,

    /// <summary>While the pointer is over the bar; a screen with no pointer still opens it on a press.</summary>
    Hover
}
