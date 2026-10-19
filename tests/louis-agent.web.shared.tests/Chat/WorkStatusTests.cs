using LouisAgent.Web.Shared.Chat;
using LouisAgent.Web.Shared.Text;

namespace LouisAgent.Web.Shared.Tests.Chat;

// T1: the line under a running turn (F34): a role's phases while its specialist works ("Soek bestanddele", then "Bou
// resep" for Puls's cook), else the app's plain "busy".
public class WorkStatusTests
{
    private static readonly ChatText Text = new()
    {
        Processing = "BESIG",
        RolePhases = new Dictionary<string, RolePhases> { ["puls-cook"] = new("Soek bestanddele", "Bou resep") },
    };

    private static Block RunningCall(string role = "puls-cook") => new()
    {
        Kind = BlockKind.Tool, ToolUseId = "call_1", ToolName = Delegation.ToolName,
        ToolInput = $$"""{"agent":"puls","toolsets":["web"],"message":"Kook","role":"{{role}}"}""",
        SubTurn = new Turn(),
    };

    [Test]
    public void NothingYet_IsBusy() =>
        Assert.That(WorkStatus.Label(new Turn(), Text), Is.EqualTo("BESIG"));

    [Test]
    public void ASpecialistJustStarted_IsGathering()
    {
        var turn = new Turn { Blocks = [RunningCall()] };

        Assert.That(WorkStatus.Label(turn, Text), Is.EqualTo("Soek bestanddele"));
    }

    [Test]
    public void ASpecialistSearching_IsGathering()
    {
        var call = RunningCall();
        call.SubTurn!.Blocks.Add(new Block { Kind = BlockKind.Tool, ToolName = "WebSearch" });
        var turn = new Turn { Blocks = [call] };

        Assert.That(WorkStatus.Label(turn, Text), Is.EqualTo("Soek bestanddele"));
    }

    [Test]
    public void ASpecialistWritingAfterAResult_IsBuilding()
    {
        var call = RunningCall();
        call.SubTurn!.Blocks.Add(new Block { Kind = BlockKind.Tool, ToolName = "FindProducts", ToolResult = "Products at Checkers…" });
        call.SubTurn.Blocks.Add(new Block { Kind = BlockKind.Text, Text = "### Die gereg" });
        var turn = new Turn { Blocks = [call] };

        Assert.That(WorkStatus.Label(turn, Text), Is.EqualTo("Bou resep"));
    }

    [Test]
    public void ASpecialistSearchingAgain_IsGatheringAgain()
    {
        var call = RunningCall();
        call.SubTurn!.Blocks.Add(new Block { Kind = BlockKind.Tool, ToolName = "FindProducts", ToolResult = "…" });
        call.SubTurn.Blocks.Add(new Block { Kind = BlockKind.Tool, ToolName = "SearchSpecials" });
        var turn = new Turn { Blocks = [call] };

        Assert.That(WorkStatus.Label(turn, Text), Is.EqualTo("Soek bestanddele"));
    }

    [Test]
    public void ARoleWithoutPhases_IsBusy()
    {
        var turn = new Turn { Blocks = [RunningCall("puls-researcher")] };

        Assert.That(WorkStatus.Label(turn, Text), Is.EqualTo("BESIG"));
    }

    [Test]
    public void AFinishedCall_IsBusy_ThePageWritesItsOwnAnswerNow()
    {
        var call = RunningCall();
        call.ToolResult = "Bobotie.\n\n[Sub-session on puls (web; puls-cook): 3 requests, $0.01]";
        var turn = new Turn { Blocks = [call] };

        Assert.That(WorkStatus.Label(turn, Text), Is.EqualTo("BESIG"));
    }
}
