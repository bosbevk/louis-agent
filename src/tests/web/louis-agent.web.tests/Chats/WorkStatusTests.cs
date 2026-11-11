using LouisAgent.Web.Chats;
using LouisAgent.Web.Text;
using LouisAgent.Testing;

namespace LouisAgent.Web.Tests.Chats;

/// <summary>The line under a running turn: a role's phases while its specialist works, else a plain "busy".</summary>
public class WorkStatusTests
{
    private static readonly ChatText Text = new()
    {
        Processing = "BESIG",
        RolePhases = new Dictionary<string, RolePhases> { [SampleRole.Alpha] = new("Soek bestanddele", "Bou resep") },
    };

    /// <summary>An ask still running, its child's stream started under this role.</summary>
    private static Block RunningCall(string role = SampleRole.Alpha) => new()
    {
        Kind = BlockKind.Tool,
        ToolUseId = "call_1",
        ToolName = Delegation.AskToolName,
        ToolInput = """{"address":"acme.cook","message":"Kook"}""",
        SubTurn = new Turn { Role = role }
    };

    /// <summary>Nothing yet is busy.</summary>
    [Test]
    public void NothingYet_IsBusy() =>
        Assert.That(WorkStatus.Label(new Turn(), Text), Is.EqualTo("BESIG"));

    /// <summary>A specialist that's just started is gathering.</summary>
    [Test]
    public void ASpecialistJustStarted_IsGathering()
    {
        var turn = new Turn { Blocks = [RunningCall()] };

        Assert.That(WorkStatus.Label(turn, Text), Is.EqualTo("Soek bestanddele"));
    }

    /// <summary>A specialist searching is gathering.</summary>
    [Test]
    public void ASpecialistSearching_IsGathering()
    {
        var call = RunningCall();
        call.SubTurn!.Blocks.Add(new Block
        {
            Kind = BlockKind.Tool,
            ToolName = "WebSearch"
        });
        var turn = new Turn { Blocks = [call] };

        Assert.That(WorkStatus.Label(turn, Text), Is.EqualTo("Soek bestanddele"));
    }

    /// <summary>A specialist writing after a result is building.</summary>
    [Test]
    public void ASpecialistWritingAfterAResult_IsBuilding()
    {
        var call = RunningCall();
        call.SubTurn!.Blocks.Add(new Block
        {
            Kind = BlockKind.Tool,
            ToolName = "FindProducts",
            ToolResult = "Products at Checkers…"
        });
        call.SubTurn.Blocks.Add(new Block
        {
            Kind = BlockKind.Text,
            Text = "### Die gereg"
        });
        var turn = new Turn { Blocks = [call] };

        Assert.That(WorkStatus.Label(turn, Text), Is.EqualTo("Bou resep"));
    }

    /// <summary>A specialist searching again goes back to gathering.</summary>
    [Test]
    public void ASpecialistSearchingAgain_IsGatheringAgain()
    {
        var call = RunningCall();
        call.SubTurn!.Blocks.Add(new Block
        {
            Kind = BlockKind.Tool,
            ToolName = "FindProducts",
            ToolResult = "…"
        });
        call.SubTurn.Blocks.Add(new Block
        {
            Kind = BlockKind.Tool,
            ToolName = "SearchSpecials"
        });
        var turn = new Turn { Blocks = [call] };

        Assert.That(WorkStatus.Label(turn, Text), Is.EqualTo("Soek bestanddele"));
    }

    /// <summary>A role with no phases is busy.</summary>
    [Test]
    public void ARoleWithoutPhases_IsBusy()
    {
        var turn = new Turn { Blocks = [RunningCall(SampleRole.Beta)] };

        Assert.That(WorkStatus.Label(turn, Text), Is.EqualTo("BESIG"));
    }

    /// <summary>A finished call is busy, since the page is writing its own answer now.</summary>
    [Test]
    public void AFinishedCall_IsBusy_ThePageWritesItsOwnAnswerNow()
    {
        var call = RunningCall();
        call.ToolResult = "Bobotie.\n\n[Answered by acme.cook, did: no tools]";
        var turn = new Turn { Blocks = [call] };

        Assert.That(WorkStatus.Label(turn, Text), Is.EqualTo("BESIG"));
    }
}
