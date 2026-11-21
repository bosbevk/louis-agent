using LouisAgent.Web.Content;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Usage;
using LouisAgent.Web.Scripts;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ChatModel = LouisAgent.Web.Chats.Chat;

namespace LouisAgent.Web.Components.Chat.Panels;

/// <summary>The work panel: the work this chat handed out, newest first.</summary>
public partial class WorkPanel : IAsyncDisposable
{
    private JsModule? _shell;

    [Inject] private IJSRuntime JS { get; set; } = null!;

    /// <summary>The shell's module (CL31), shared with every part of the chat; the browser fetches it once.</summary>
    private JsModule Shell => _shell ??= new JsModule(JS, FrameworkScripts.Shell);

    [Parameter] public ChatModel? Chat { get; set; }

    /// <summary>The site's picture for an empty panel; with none, there isn't one.</summary>
    [Parameter]
    public string? EmptyImage { get; set; }

    /// <summary>The site's picture beside each piece of work; with none, there isn't one.</summary>
    [Parameter]
    public string? Avatar { get; set; }

    [Parameter] public bool Streaming { get; set; }
    [Parameter] public CurrencyChoice? Currency { get; set; }

    /// <param name="Who">The role the work went to (F26; Louis: "show oorgedra → role"), else the peer.</param>
    private sealed record Item(Delegation Delegation, Block Block, string Who, string Status);

    /// <summary>Every delegation in the chat, newest first; the one still running is "running".</summary>
    private List<Item> Items
    {
        get
        {
            if (Chat is null) return [];
            var items = new List<Item>();
            foreach (var (turn, live) in Chat.Turns.Select((turn, t) => (turn, Streaming && t == Chat.Turns.Count - 1)))
                foreach (var block in turn.Blocks)
                {
                    if (Delegation.From(block) is not { } delegation) continue;
                    string agent = delegation.Agent is { Length: > 0 } named
                        ? named
                        : block.SubAgent ?? delegation.ResultAgent ?? Text.Specialist;
                    string who = delegation.Role ?? agent;
                    items.Add(new Item(delegation, block, who, StatusOf(live, block, delegation)));
                }

            items.Reverse();
            return items;
        }
    }

    /// <summary>A piece of work's state: still working in the live turn, failed, or done.</summary>
    /// <remarks>A switch on the shape (ADR 0051), one arm per state, the first that holds winning.</remarks>
    private static string StatusOf(bool live, Block block, Delegation delegation) =>
        (Working: live && block.ToolResult is null, Failed: block.ToolFailed || delegation.Failed) switch
        {
            { Working: true } => "running",
            { Failed: true } => "failed",
            _ => "done"
        };

    /// <summary>A page's address as people read it: no scheme, no www.</summary>
    private static string Address(string url) =>
        url.Replace("https://", "").Replace("http://", "").Replace("www.", "").TrimEnd('/');

    /// <summary>Every source of the chat, newest turn first, each address once.</summary>
    private List<Source> ChatSources =>
        Chat is null ? [] : Chat.Turns.AsEnumerable().Reverse().SelectMany(Sources.Of).DistinctBy(s => s.Url).ToList();

    /// <summary>The specialists' usage in this chat: every delegation's sub-turn, added up.</summary>
    private TurnUsage? SpecialistUsage =>
        Items.Select(i => i.Block.SubTurn?.Usage).OfType<TurnUsage>()
            .Aggregate((TurnUsage?)null, (sum, u) => sum is null ? u : sum + u);


    /// <summary>Hides the work panel.</summary>
    private Task HideWork() => Shell.InvokeVoidAsync("toggleWork").AsTask();

    /// <summary>Lets the shell's module go, if it was ever loaded.</summary>
    public ValueTask DisposeAsync() => _shell?.DisposeAsync() ?? ValueTask.CompletedTask;
}
