using LouisAgent.Web.Chats;
using LouisAgent.Web.Shell;
using LouisAgent.Web.Usage;

namespace LouisAgent.Web.Tests.Shell;

/// <summary>
/// What a chat and a month cost, specialists included, since a chat's own usage doesn't hold theirs yet. Nothing spent
/// reads as nothing, and the month is the person's own calendar month.
/// </summary>
public class ChatCostsTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 6, 12, 0, 0, TimeSpan.FromHours(2));

    /// <summary>A chat's total adds its own requests and its specialists'.</summary>
    [Test]
    public void AChatsTotal_AddsItsSpecialists()
    {
        var chat = Chat(Now, Turn(1.00m, specialist: 0.50m), Turn(0.25m));

        Assert.That(ChatCosts.Total(chat)!.Cost, Is.EqualTo(1.75m));
    }

    /// <summary>A chat with no usage has no total.</summary>
    [Test]
    public void NoUsage_IsNoTotal() =>
        Assert.That(ChatCosts.Total(Chat(Now)), Is.Null);

    /// <summary>The month's chats are those used in the same calendar month, in the person's own time.</summary>
    [Test]
    public void TheMonth_IsTheCalendarMonth_InThePersonsTime()
    {
        var thisMonth = Chat(Now.AddDays(-5));
        var lastMonth = Chat(Now.AddMonths(-1));
        var lastYear = Chat(Now.AddYears(-1));
        // 23:30 UTC on 31 October is already 1 November at UTC+2.
        var justInTime = Chat(new DateTimeOffset(2026, 10, 31, 23, 30, 0, TimeSpan.Zero));

        var month = ChatCosts.InMonth([thisMonth, lastMonth, lastYear, justInTime], Now);

        Assert.That(month, Is.EquivalentTo(new[] { thisMonth, justInTime }));
    }

    /// <summary>The month's total adds its chats.</summary>
    [Test]
    public void TheMonthsTotal_AddsItsChats()
    {
        var total = ChatCosts.Total([Chat(Now, Turn(1.00m)), Chat(Now, Turn(2.00m, specialist: 0.50m)), Chat(Now)]);

        Assert.That(total!.Cost, Is.EqualTo(3.50m));
    }

    /// <summary>Nothing spent reads as nothing, not as unknown.</summary>
    [Test]
    public void NothingSpent_ReadsAsNothing() =>
        Assert.That(ChatCosts.Say(null, null), Is.EqualTo("$0"));

    /// <summary>A cost reads in the currency it's shown in.</summary>
    [Test]
    public void ACost_ReadsAsItIsShown() =>
        Assert.That(ChatCosts.Say(Usage(1.50m), null), Is.EqualTo("$1.50"));

    /// <summary>A chat used at this time, with these turns.</summary>
    private static Chat Chat(DateTimeOffset used, params Turn[] turns) =>
        new() { SessionId = Guid.NewGuid().ToString(), UpdatedAt = used, Turns = [.. turns] };

    /// <summary>A turn that cost this, with a specialist that cost that.</summary>
    private static Turn Turn(decimal cost, decimal? specialist = null)
    {
        var turn = new Turn { Usage = Usage(cost) };
        if (specialist is { } own) turn.Blocks.Add(new Block { Kind = BlockKind.Tool, SubTurn = new Turn { Usage = Usage(own) } });
        return turn;
    }

    /// <summary>One priced request.</summary>
    private static TurnUsage Usage(decimal cost) => new(1, 1, 0, 0, 1, cost, "USD", 0);
}
