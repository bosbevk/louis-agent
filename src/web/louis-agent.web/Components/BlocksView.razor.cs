using LouisAgent.Web.Chats;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components;

public partial class BlocksView
{
    [Parameter, EditorRequired] public List<Block> Blocks { get; set; } = default!;
    [Parameter] public bool Streaming { get; set; }
    [Parameter] public CurrencyChoice? Currency { get; set; }

    /// <summary>The images a tool gave in this turn (FX22): the only ones an answer here may show besides a shop's own photos.</summary>
    [Parameter] public IReadOnlySet<string>? Shown { get; set; }
}
