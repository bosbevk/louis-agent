using LouisAgent.Choices;
using LouisAgent.Composition.Sites;

namespace LouisAgent.Packs.Site.Messages.Holes.Pages;

/// <summary>One place found: its address, its heading and its kind, as the guide reads it.</summary>
public sealed record PlaceLineHoles(string Address, string Heading, string Kind)
{
    /// <summary>The line's holes from a place.</summary>
    public static PlaceLineHoles Of(SitePlace place) => new(place.Address, place.Heading, place.Kind.Wire());
}
