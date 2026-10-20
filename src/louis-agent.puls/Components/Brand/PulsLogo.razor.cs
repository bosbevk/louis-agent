using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Brand;

public partial class PulsLogo
{
    private readonly string _id = "puls-" + Guid.NewGuid().ToString("N")[..8] + "-";
}
