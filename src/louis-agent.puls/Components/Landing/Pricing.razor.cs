using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Landing;

public partial class Pricing
{
    // What a question cost, as the price card's examples (rand).
    private static readonly (string Question, string Cost)[] Examples =
    [
        ("Wat is nuut in .NET 10?", "0.47"), ("Som die README op", "0.09"), ("Laaste drie commits", "0.12"),
    ];
}
