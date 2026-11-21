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
    [Parameter]
    public IReadOnlySet<string>? Shown { get; set; }

    /// <summary>
    /// Whether the answer shows its work: what it thought and the tools it called. Off, a visitor sees the words and a
    /// thinking label alone (F128.1), and nothing else is drawn, so it isn't in the page to read either.
    /// </summary>
    [Parameter]
    public bool ShowWork { get; set; } = true;

    /// <summary>What a thinking block says on its label: thinking now, or how long it thought.</summary>
    /// <summary>
    /// The visitor's message after this answer, once there is one, so choices offered in it fold to the one chosen.
    /// </summary>
    [Parameter]
    public string? Answer { get; set; }

    /// <summary>What a click on an offered card does with its label: the chat sends it as the visitor's message.</summary>
    [Parameter]
    public EventCallback<string> Chosen { get; set; }

    private string ThinkingLabel(Block block) => block.ThinkingSeconds is { } seconds
        ? string.Format(Text.ThoughtFor, Math.Max(1, Math.Round(seconds)))
        : Text.Thinking;
}
