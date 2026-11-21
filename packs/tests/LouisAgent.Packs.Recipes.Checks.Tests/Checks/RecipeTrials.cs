using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Evals;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;

namespace LouisAgent.Packs.Recipes.Tests.Checks;

/// <summary>
/// Trials built from canned stream events, the way the recipe rule's tests need them: an answer's text, turn by turn,
/// with no identity's names.
/// </summary>
internal static class RecipeTrials
{
    /// <summary>A mode with this pack's toolset: an agent that can read the recipe sites.</summary>
    public static readonly EvalMode Mode = new("cooking", "cook", [RecipesToolset.Recipes.Wire()]);

    /// <summary>A trial of a scenario that expects nothing, made of these turns.</summary>
    public static Trial Trial(params TurnTranscript[] turns) =>
        new("run_1", Mode, 1,
            new EvalScenario("recipe-x", new EvalInput(null, "Africa/Johannesburg", ["x"]),
                JsonDocument.Parse("{}").RootElement.Clone(), [], DatasetSplit.Dev),
            new Transcript(turns));

    /// <summary>One turn of the conversation, made of these events, failed or not.</summary>
    public static TurnTranscript Turn(int number, IReadOnlyList<SessionEvent> events, TurnFailure? failure = null) =>
        new(number, "sess_1", "q", events, TimeSpan.FromSeconds(1), failure);

    /// <summary>An error the turn's stream reported: the agent's own, not the infrastructure's.</summary>
    public static SessionEvent Error() =>
        SessionEvent.From("error",
            JsonDocument.Parse("""{"type":"error","error":{"type":"api_error","message":"overloaded"}}""")
                .RootElement.Clone());

    /// <summary>
    /// A piece of the answer's text, as the stream carries it; the text is written as a JSON string, so quotes and line
    /// breaks in it stay text.
    /// </summary>
    public static SessionEvent Text(string text) =>
        SessionEvent.From("content_block_delta",
            JsonDocument.Parse("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":TEXT}}"""
                .Replace("TEXT", JsonSerializer.Serialize(text))).RootElement.Clone());
}
