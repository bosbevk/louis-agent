using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Brand;

public partial class PulsTile
{
    private readonly string _id = "tile-" + Guid.NewGuid().ToString("N")[..8] + "-";
}
