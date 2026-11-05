using LouisAgent.Web.Shared.Chats;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Shared.Components;

public partial class DelegationCard
{
    [Parameter, EditorRequired] public Delegation Delegation { get; set; } = default!;
    [Parameter, EditorRequired] public Block Block { get; set; } = default!;
    [Parameter] public bool Streaming { get; set; }
    [Parameter] public CurrencyChoice? Currency { get; set; }

    /// <summary>The images a tool gave in this turn (FX22): the only ones an answer here may show besides a shop's own photos.</summary>
    [Parameter] public IReadOnlySet<string>? Shown { get; set; }

    private bool Running => Streaming && Block.ToolResult is null;

    private string Status => Running ? "running" : Block.ToolFailed || Delegation.Failed ? "failed" : "done";

    private string StatusText => Status switch { "running" => Text.Working, "failed" => Text.Failed, _ => Text.Done };

    /// <summary>The peer actually used: named in the call, else reported by its stream or its result (an empty name means the only peer).</summary>
    private string Agent => Delegation.Agent is { Length: > 0 } named ? named : Block.SubAgent ?? Delegation.ResultAgent ?? Text.Specialist;

    /// <summary>Who the work went to, as the user thinks of it: the role (F26; Louis: "show oorgedra → role"), else the handle of a continued one, else the peer.</summary>
    private string Who => Delegation.Role ?? Delegation.Handle ?? Agent;

    private string Initial => Delegation.Minion ? "m" : Who[..1].ToUpperInvariant();

    /// <summary>The cost shown on the rolled-up line.</summary>
    private string? CostLine => Block.SubTurn?.Usage is { } usage ? UsageFormat.Cost(usage, Currency) : null;

    private string Steps(Turn sub) =>
        Text.Steps(sub.Blocks.Count(b => b.Kind == BlockKind.Tool), sub.Blocks.Count(b => b.Kind == BlockKind.Thinking));
}
