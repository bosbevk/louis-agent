using LouisAgent.Web.Chats;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Chat.Panels;

/// <summary>One step of a specialist's work: a tool call, or a minion it handed one page to.</summary>
public partial class WorkStep
{
    [Inject] private ChatText Text { get; set; } = null!;

    /// <summary>The tool call this step is.</summary>
    [Parameter, EditorRequired] public Block Step { get; set; } = default!;

    /// <summary>The currency a minion's cost shows in.</summary>
    [Parameter] public CurrencyChoice? Currency { get; set; }

    /// <summary>A tool call's input on one line, without its braces.</summary>
    private static string Summarise(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return "";
        string flat = string.Join(" ", json.Split('\n', StringSplitOptions.TrimEntries)).Trim('{', '}', ' ');
        return flat.Length > 60 ? flat[..60] + "…" : flat;
    }
}
