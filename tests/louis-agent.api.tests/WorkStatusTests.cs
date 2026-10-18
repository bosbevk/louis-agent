using LouisAgent.Web.Shared;

namespace LouisAgent.Api.Tests;

// F34, Louis, on "BESIG" under the cook's card while it searched: "it must actually say soek bestanddele en dan bou
// resep, maybe some small loader icon left of it". The line under a running turn says what the specialist is doing:
// its role's phases (an app's own words), searching until it has results and is writing, then building.
public class WorkStatusTests
{
    private static readonly ChatText Text = new()
    {
        Processing = "BESIG",
        RolePhases = new Dictionary<string, RolePhases> { ["puls-cook"] = new("Soek bestanddele", "Bou resep") },
    };

    private static Turn Running(string role, params Block[] specialist) => new()
    {
        Blocks =
        [
            new Block { Kind = BlockKind.Text, Text = "Ek gee dit gou vir my kok." },
            new Block
            {
                Kind = BlockKind.Tool, ToolName = Delegation.ToolName,
                ToolInput = $$"""{"toolsets":["web"],"message":"**Vir:** kok","role":"{{role}}"}""",
                SubTurn = new Turn { Blocks = [.. specialist] },
            },
        ],
    };

    private static Block Tool(string name, string? result) => new() { Kind = BlockKind.Tool, ToolName = name, ToolResult = result };

    [Test]
    public void TheCook_SearchingTheWeb_IsLookingForIngredients() =>
        Assert.That(WorkStatus.Label(Running("puls-cook", new Block { Kind = BlockKind.Thinking }, Tool("WebSearch", "Search results …")), Text),
            Is.EqualTo("Soek bestanddele"), "Louis's screenshot: a WebSearch done, the cook still at it");

    [Test]
    public void TheCook_JustStarted_IsLookingForIngredients() =>
        Assert.That(WorkStatus.Label(Running("puls-cook", new Block { Kind = BlockKind.Thinking }), Text), Is.EqualTo("Soek bestanddele"),
            "thinking before any tool: it hasn't found anything yet");

    [Test]
    public void TheCook_WritingAfterItsSearches_IsBuildingTheRecipe()
    {
        Assert.That(WorkStatus.Label(Running("puls-cook", Tool("FindProducts", "Products at Checkers …"), new Block { Kind = BlockKind.Thinking }), Text),
            Is.EqualTo("Bou resep"));
        Assert.That(WorkStatus.Label(Running("puls-cook", Tool("FindProducts", "Products at Checkers …"), new Block { Kind = BlockKind.Text, Text = "## Die gereg" }), Text),
            Is.EqualTo("Bou resep"));
    }

    [Test]
    public void TheCook_SearchingAgainAfterThinking_IsLookingForIngredientsAgain() =>
        Assert.That(WorkStatus.Label(Running("puls-cook", Tool("FindProducts", "…"), new Block { Kind = BlockKind.Thinking }, Tool("FindProducts", null)), Text),
            Is.EqualTo("Soek bestanddele"));

    [Test]
    public void ARoleWithoutPhases_OrNoSpecialist_IsJustBusy()
    {
        Assert.That(WorkStatus.Label(Running("puls-other", Tool("WebSearch", "…")), Text), Is.EqualTo("BESIG"));
        Assert.That(WorkStatus.Label(new Turn { Blocks = [Tool("WebSearch", null)] }, Text), Is.EqualTo("BESIG"));
        Assert.That(WorkStatus.Label(new Turn(), Text), Is.EqualTo("BESIG"));
    }
}
