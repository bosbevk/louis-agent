using LouisAgent.Evals.Trials;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;
using static LouisAgent.Evals.Tests.Trials.ToolEvents;

namespace LouisAgent.Evals.Tests.Trials;

/// <summary>
/// What one named tool gave back in a turn: matched to its call by id, the agent's and its specialists' together, in
/// the order the calls were made.
/// </summary>
public class ResultsOfToolTests
{
    /// <summary>A specialist's call, one level down, counts as the agent's own does.</summary>
    [Test]
    public void ASpecialistsResults_AreFound_OneLevelDown()
    {
        var turn = Turn(1, [SubUse("s1", "ReadRecipe"), SubGot("s1", "the print")]);

        Assert.That(EngineEvents.ResultsOfTool(turn, "ReadRecipe"), Is.EqualTo(new[] { "the print" }));
    }

    /// <summary>
    /// The agent's calls and its specialist's are in the order they were made, not the agent's first, and a result
    /// that arrives late still belongs to its own call.
    /// </summary>
    [Test]
    public void TheResults_AreInTheOrderTheCallsWereMade_EachWithItsOwn()
    {
        var turn = Turn(1,
        [
            Use("a1", "ReadRecipe"), SubUse("s1", "ReadRecipe"), SubGot("s1", "second"), Got("a1", "first"),
            Use("a2", "ReadRecipe"), Got("a2", "third")
        ]);

        Assert.That(EngineEvents.ResultsOfTool(turn, "ReadRecipe"), Is.EqualTo(new[] { "first", "second", "third" }));
    }

    /// <summary>Another tool's results are left out, and a tool never called has none.</summary>
    [Test]
    public void AnotherToolsResults_AreLeftOut_AndATooNeverCalledHasNone()
    {
        var turn = Turn(1, [SubUse("s1", "WebSearch"), SubGot("s1", "search results")]);

        Assert.That(EngineEvents.ResultsOfTool(turn, "ReadRecipe"), Is.Empty);
        Assert.That(EngineEvents.ResultsOfTool(turn, "WebSearch"), Is.EqualTo(new[] { "search results" }));
    }

    /// <summary>
    /// A call whose result never came adds nothing, and a result with no call of that tool is no result.
    /// </summary>
    [Test]
    public void ACallWithNoResult_AddsNothing()
    {
        var turn = Turn(1,
            [Use("a1", "ReadRecipe"), Got("a2", "someone else's"), Use("a3", "ReadRecipe"), Got("a3", "ok")]);

        Assert.That(EngineEvents.ResultsOfTool(turn, "ReadRecipe"), Is.EqualTo(new[] { "ok" }));
    }
}
