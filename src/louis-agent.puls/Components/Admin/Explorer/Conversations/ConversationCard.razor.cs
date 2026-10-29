using LouisAgent.Puls.Admin.Explorer.Text;
using LouisAgent.Puls.Admin.Explorer.Tree;
using LouisAgent.Puls.Admin.Grades;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Explorer.Conversations;

/// <summary>
/// One mode's conversation of a task as a card: its question and cost, its review pill, chips and grades, and a strip
/// of its cost split between Puls and the specialist.
/// </summary>
public partial class ConversationCard
{
    /// <summary>The conversation.</summary>
    [Parameter, EditorRequired]
    public ConversationNode Conversation { get; set; } = null!;

    /// <summary>The mode it ran in.</summary>
    [Parameter, EditorRequired]
    public ModeNode Mode { get; set; } = null!;

    /// <summary>The conversation's key in the run.</summary>
    [Parameter, EditorRequired]
    public ConversationRef Where { get; set; } = null!;

    /// <summary>Where it stands for this reviewer.</summary>
    [Parameter]
    public ReviewState Status { get; set; }

    /// <summary>The first question asked for its task.</summary>
    [Parameter]
    public string? Question { get; set; }

    /// <summary>The run's graders, for the order of the grade chips.</summary>
    [Parameter]
    public IReadOnlyList<GraderRow>? Graders { get; set; }

    /// <summary>The dearest conversation's cost, so every strip is drawn on one scale.</summary>
    [Parameter]
    public decimal TopCost { get; set; }

    /// <summary>The currency to show money in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>Someone wants the chats side by side on this conversation.</summary>
    [Parameter]
    public EventCallback<string> OnReplay { get; set; }

    /// <summary>Someone opened this conversation's turns.</summary>
    [Parameter]
    public EventCallback<string> OnOpenConversation { get; set; }

    /// <summary>What Puls's own requests cost.</summary>
    private decimal? Own => RequestSums.Cost(Conversation.Rows, specialist: false);

    /// <summary>What the specialist's requests cost.</summary>
    private decimal? Spec => RequestSums.Cost(Conversation.Rows, specialist: true);

    /// <summary>The strip's hover: the task, and each side's cost.</summary>
    private string StripTitle =>
        $"{Conversation.Task}: Puls {Money(Own)}, {ModeNames.Roles([Mode])} {Money(Spec)}";

    /// <summary>A cost in the chosen currency, or a dash.</summary>
    private string Money(decimal? usd) => ExplorerText.Money(usd, Currency);
}
