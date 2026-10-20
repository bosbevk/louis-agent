using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Shared.Components.Chat;

public partial class ChatUsageBar
{
    [Parameter] public TurnUsage? Usage { get; set; }
    [Parameter] public CurrencyChoice? Currency { get; set; }

    /// <summary>The line's words, given the usage; the shared "This chat: …" total when null.</summary>
    [Parameter] public RenderFragment<TurnUsage>? ChildContent { get; set; }
}
