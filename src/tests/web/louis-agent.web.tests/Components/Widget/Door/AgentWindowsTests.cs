using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Widget.Door;

namespace LouisAgent.Web.Tests.Components.Widget.Door;

/// <summary>
/// Which window on a page shows which agent (F127.4.2): a showroom's live window says the business it shows, and the
/// guide's widget hands it an ask to that business, so the visitor watches the talk in the business's own window. An
/// ask to another agent, or to a role of the business, is the guide's own.
/// </summary>
public class AgentWindowsTests
{
    /// <summary>The guide's ask, as the stream gives it.</summary>
    private static Block Ask(string address) => new()
    {
        Kind = BlockKind.Tool,
        ToolName = Delegation.AskToolName,
        ToolInput = $$"""{"address":"{{address}}","message":"A room for Friday?"}"""
    };

    /// <summary>An ask to the business a window shows goes to that window.</summary>
    [Test]
    public void AnAskToTheBusinessAWindowShows_GoesToIt()
    {
        var windows = new AgentWindows();
        Block? heard = null;
        windows.Asked += call => heard = call;
        using var _ = windows.Show("seemeeu-guesthouse");
        var ask = Ask("seemeeu-guesthouse");

        bool taken = windows.Take(ask);

        Assert.Multiple(() =>
        {
            Assert.That(taken, Is.True);
            Assert.That(heard, Is.SameAs(ask));
        });
    }

    /// <summary>An ask to another agent, a role of the business, or any other call stays the guide's.</summary>
    [TestCase("koringland-attorneys")]
    [TestCase("seemeeu-guesthouse.cook")]
    public void AnyOtherAsk_StaysTheGuides(string address)
    {
        var windows = new AgentWindows();
        using var _ = windows.Show("seemeeu-guesthouse");

        Assert.Multiple(() =>
        {
            Assert.That(windows.Take(Ask(address)), Is.False);
            Assert.That(windows.Take(new Block
            {
                Kind = BlockKind.Tool,
                ToolName = "ShowPage",
                ToolInput = "{}"
            }),
                Is.False);
        });
    }

    /// <summary>A window that closes no longer shows its business: the guide keeps its asks again.</summary>
    [Test]
    public void AClosedWindow_ShowsNothing()
    {
        var windows = new AgentWindows();
        windows.Show("seemeeu-guesthouse").Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(windows.Shows("seemeeu-guesthouse"), Is.False);
            Assert.That(windows.Take(Ask("seemeeu-guesthouse")), Is.False);
        });
    }

    /// <summary>The guide's stream moving on tells the windows, so the one showing the talk draws again.</summary>
    [Test]
    public void TheGuidesStreamMovingOn_TellsTheWindows()
    {
        var windows = new AgentWindows();
        int told = 0;
        windows.Moved += () => told++;

        windows.Move();

        Assert.That(told, Is.EqualTo(1));
    }
}
