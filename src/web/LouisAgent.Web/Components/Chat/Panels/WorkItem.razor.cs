using LouisAgent.Web.Chats;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Chat.Panels;

/// <summary>One piece of work a chat handed out, with its steps as they happen and what it cost.</summary>
public partial class WorkItem
{
    [Inject] private ChatText Text { get; set; } = null!;

    /// <summary>What the call said: its toolsets, its task and whether it continued one.</summary>
    [Parameter, EditorRequired] public Delegation Delegation { get; set; } = default!;

    /// <summary>The call itself, holding the specialist's own turn.</summary>
    [Parameter, EditorRequired] public Block Block { get; set; } = default!;

    /// <summary>The role the work went to, else the peer.</summary>
    [Parameter, EditorRequired] public string Who { get; set; } = "";

    /// <summary>The card's state: <c>running</c>, <c>failed</c> or <c>done</c>.</summary>
    [Parameter, EditorRequired] public string Status { get; set; } = "";

    /// <summary>The site's picture beside it; with none, there isn't one.</summary>
    [Parameter] public string? Avatar { get; set; }

    /// <summary>The currency its costs show in.</summary>
    [Parameter] public CurrencyChoice? Currency { get; set; }

    /// <summary>The specialist's tool calls so far.</summary>
    private List<Block> Steps => Block.SubTurn?.Blocks.Where(b => b.Kind == BlockKind.Tool).ToList() ?? [];

    /// <summary>How many times the specialist has stopped to think.</summary>
    private int Thoughts => Block.SubTurn?.Blocks.Count(b => b.Kind == BlockKind.Thinking) ?? 0;
}
