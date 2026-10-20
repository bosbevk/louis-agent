using LouisAgent.Puls.Content;
using LouisAgent.Web.Shared.Chats;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;
using ChatModel = LouisAgent.Web.Shared.Chats.Chat;

namespace LouisAgent.Puls.Components.Chat.Panels;

public partial class WorkPanel
{
    [Parameter] public ChatModel? Chat { get; set; }
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
            // A continue names only its handle until its footer arrives; the role is the one the start with that handle had.
            var roles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var items = new List<Item>();
            foreach (var (turn, live) in Chat.Turns.Select((turn, t) => (turn, Streaming && t == Chat.Turns.Count - 1)))
            foreach (var block in turn.Blocks)
            {
                if (Delegation.From(block) is not { } delegation) continue;
                if (delegation is { Handle: { } h, Role: { } r }) roles[h] = r;
                string agent = delegation.Agent is { Length: > 0 } named ? named : block.SubAgent ?? delegation.ResultAgent ?? Text.Specialist;
                string who = delegation.Role ?? (delegation.Handle is { } handle && roles.TryGetValue(handle, out var known) ? known : null) ?? delegation.Handle ?? agent;
                string status = live && block.ToolResult is null ? "running" : block.ToolFailed || delegation.Failed ? "failed" : "done";
                items.Add(new Item(delegation, block, who, status));
            }
            items.Reverse();
            return items;
        }
    }

    /// <summary>A page's address as people read it: no scheme, no www.</summary>
    private static string Address(string url) => url.Replace("https://", "").Replace("http://", "").Replace("www.", "").TrimEnd('/');

    /// <summary>Every source of the chat, newest turn first, each address once.</summary>
    private List<Source> ChatSources =>
        Chat is null ? [] : Chat.Turns.AsEnumerable().Reverse().SelectMany(Sources.Of).DistinctBy(s => s.Url).ToList();

    /// <summary>The specialists' usage in this chat: every delegation's sub-turn, added up.</summary>
    private TurnUsage? SpecialistUsage =>
        Items.Select(i => i.Block.SubTurn?.Usage).OfType<TurnUsage>().Aggregate((TurnUsage?)null, (sum, u) => sum is null ? u : sum + u);

    private static string Summarise(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return "";
        string flat = string.Join(" ", json.Split('\n', StringSplitOptions.TrimEntries)).Trim('{', '}', ' ');
        return flat.Length > 60 ? flat[..60] + "…" : flat;
    }
}
