using LouisAgent.Choices;

namespace LouisAgent.FilmExport.Cards;

/// <summary>The share card's ground (FX174, Louis: "and the light theme one?"): light, as the site opens, or dark.</summary>
public enum CardLook
{
    /// <summary>A light ground with dark words, the site's default look.</summary>
    [WireName("light")] Light,

    /// <summary>A dark ground with white words.</summary>
    [WireName("dark")] Dark
}
