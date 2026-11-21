using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Brand;

public partial class Water
{
    private readonly string _id = "water-" + Guid.NewGuid().ToString("N")[..8] + "-";
}
