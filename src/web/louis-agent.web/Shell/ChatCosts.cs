using LouisAgent.Web.Chats;
using LouisAgent.Web.Usage;

namespace LouisAgent.Web.Shell;

/// <summary>
/// What a chat and a month cost, specialists included, since a chat's own usage doesn't hold theirs yet. Nothing spent
/// reads as nothing, and the month is the person's own calendar month.
/// </summary>
public static class ChatCosts
{
    /// <summary>A chat's whole cost: its own requests and its specialists'; null when it used nothing.</summary>
    public static TurnUsage? Total(Chat chat) =>
        Sum(chat.Turns.SelectMany(turn => turn.Blocks.Select(block => block.SubTurn?.Usage).Prepend(turn.Usage)));

    /// <summary>Several chats' whole cost; null when they used nothing.</summary>
    public static TurnUsage? Total(IEnumerable<Chat> chats) => Sum(chats.Select(Total));

    /// <summary>The chats used in the same calendar month as this moment, in its own time.</summary>
    public static List<Chat> InMonth(IEnumerable<Chat> chats, DateTimeOffset now) =>
    [
        .. chats.Where(chat => chat.UpdatedAt.ToOffset(now.Offset) is var used
                               && used.Year == now.Year && used.Month == now.Month)
    ];

    /// <summary>A cost as the menu says it; none counts as nothing spent.</summary>
    public static string Say(TurnUsage? usage, CurrencyChoice? currency) => usage is null
        ? UsageFormat.Money(0, null, currency)
        : UsageFormat.Cost(usage, currency);

    /// <summary>These records added up, leaving out the ones that aren't there; null when none is.</summary>
    private static TurnUsage? Sum(IEnumerable<TurnUsage?> usages) =>
        usages.OfType<TurnUsage>().Aggregate((TurnUsage?)null, (sum, usage) => sum is null ? usage : sum + usage);
}
