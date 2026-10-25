using LouisAgent.Web.Shared.Chats;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Shared.Components;

public partial class BlocksView
{
    [Parameter, EditorRequired] public List<Block> Blocks { get; set; } = default!;
    [Parameter] public bool Streaming { get; set; }
    [Parameter] public CurrencyChoice? Currency { get; set; }

    /// <summary>The images a tool gave in this turn (FX22): the only ones an answer here may show besides a shop's own photos.</summary>
    [Parameter] public IReadOnlySet<string>? Shown { get; set; }
}
