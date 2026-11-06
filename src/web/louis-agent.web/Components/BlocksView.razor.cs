using LouisAgent.Web.Chats;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components;

/// <summary>The blocks of an answer: thinking, text, tool calls, and a card for each delegation.</summary>
public partial class BlocksView
{
    [Parameter, EditorRequired] public List<Block> Blocks { get; set; } = default!;
    [Parameter] public bool Streaming { get; set; }
    [Parameter] public CurrencyChoice? Currency { get; set; }

    /// <summary>The images a tool gave in this turn: all an answer may show, besides a shop's own photos.</summary>
    [Parameter] public IReadOnlySet<string>? Shown { get; set; }
}
