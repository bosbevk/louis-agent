namespace LouisAgent.Web.Components.Site.Pieces;

/// <summary>What a panel's glass does to what is behind its middle, as the theme says: nothing, ripple, blur.</summary>
public enum GlassType
{
    /// <summary>Plain glass: what is behind shows as it is, bent only at the edge.</summary>
    Plain,

    /// <summary>Water: what is behind ripples.</summary>
    Water,

    /// <summary>Frost: what is behind is blurred.</summary>
    Frost
}
