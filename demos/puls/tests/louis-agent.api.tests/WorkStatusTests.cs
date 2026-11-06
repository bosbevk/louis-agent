using LouisAgent.Testing;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Text;

namespace LouisAgent.Api.Tests;

/// <summary>
/// The line under a running turn says what the specialist is doing: its role's phases (an app's own words), searching
/// until it has results and is writing, then building.
/// </summary>
public class WorkStatusTests
{
    private static readonly ChatText Text = new()
    {
        Processing = "BESIG",
        RolePhases = new Dictionary<string, RolePhases> { [SampleRole.Alpha] = new("Soek bestanddele", "Bou resep") },
    };

    /// <summary>A running turn whose specialist has these blocks.</summary>
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

    /// <summary>A tool call with this result; none means it's still running.</summary>
    private static Block Tool(string name, string? result) =>
        new() { Kind = BlockKind.Tool, ToolName = name, ToolResult = result };

    /// <summary>While the cook searches, it's looking for ingredients.</summary>
    [Test]
    public void TheCook_SearchingTheWeb_IsLookingForIngredients() =>
        Assert.That(
            WorkStatus.Label(
                Running(SampleRole.Alpha, new Block { Kind = BlockKind.Thinking },
                    Tool("WebSearch", "Search results …")), Text),
            Is.EqualTo("Soek bestanddele"), "Louis's screenshot: a WebSearch done, the cook still at it");

    /// <summary>Just started counts as looking for ingredients.</summary>
    [Test]
    public void TheCook_JustStarted_IsLookingForIngredients() =>
        Assert.That(WorkStatus.Label(Running(SampleRole.Alpha, new Block { Kind = BlockKind.Thinking }), Text),
            Is.EqualTo("Soek bestanddele"),
            "thinking before any tool: it hasn't found anything yet");

    /// <summary>Writing after its searches, it's building the recipe.</summary>
    [Test]
    public void TheCook_WritingAfterItsSearches_IsBuildingTheRecipe()
    {
        Assert.That(
            WorkStatus.Label(
                Running(SampleRole.Alpha, Tool("FindProducts", "Products at Checkers …"),
                    new Block { Kind = BlockKind.Thinking }), Text),
            Is.EqualTo("Bou resep"));
        Assert.That(
            WorkStatus.Label(
                Running(SampleRole.Alpha, Tool("FindProducts", "Products at Checkers …"),
                    new Block { Kind = BlockKind.Text, Text = "## Die gereg" }), Text),
            Is.EqualTo("Bou resep"));
    }

    /// <summary>Searching again after thinking goes back to looking for ingredients.</summary>
    [Test]
    public void TheCook_SearchingAgainAfterThinking_IsLookingForIngredientsAgain() =>
        Assert.That(
            WorkStatus.Label(
                Running(SampleRole.Alpha, Tool("FindProducts", "…"), new Block { Kind = BlockKind.Thinking },
                    Tool("FindProducts", null)), Text),
            Is.EqualTo("Soek bestanddele"));

    /// <summary>A role with no phases, or no specialist, is just busy.</summary>
    [Test]
    public void ARoleWithoutPhases_OrNoSpecialist_IsJustBusy()
    {
        Assert.That(WorkStatus.Label(Running("puls-other", Tool("WebSearch", "…")), Text), Is.EqualTo("BESIG"));
        Assert.That(WorkStatus.Label(new Turn { Blocks = [Tool("WebSearch", null)] }, Text), Is.EqualTo("BESIG"));
        Assert.That(WorkStatus.Label(new Turn(), Text), Is.EqualTo("BESIG"));
    }
}
