using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Evals;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;

namespace LouisAgent.Packs.Shops.Tests.Checks;

/// <summary>
/// Trials built from canned stream events, the way the shop rules' tests need them: an answer's text, turn by turn,
/// with no identity's names.
/// </summary>
internal static class ShopTrials
{
    /// <summary>A mode with this pack's toolset: an agent that can read the shops.</summary>
    public static readonly EvalMode Mode = new("shopping", "shopper", [ShopsToolset.Shops.Wire()]);

    /// <summary>A trial of a scenario with the given <c>expected</c> JSON, made of these turns.</summary>
    public static Trial Trial(string expected, params TurnTranscript[] turns) =>
        new("run_1", Mode, 1,
            new EvalScenario("cook-x", new EvalInput(null, "Africa/Johannesburg", ["x"]),
                JsonDocument.Parse(expected).RootElement.Clone(), [], DatasetSplit.Dev),
            new Transcript(turns));

    /// <summary>One turn of the conversation, made of these events, failed or not.</summary>
    public static TurnTranscript Turn(int number, IReadOnlyList<SessionEvent> events, TurnFailure? failure = null) =>
        new(number, "sess_1", "q", events, TimeSpan.FromSeconds(1), failure);

    /// <summary>
    /// A tool call a specialist made, as the stream carries it: inside a sub_session event, not the turn's own.
    /// </summary>
    public static SessionEvent SpecialistCall(string name, string inputJson) =>
        SessionEvent.From("sub_session",
            JsonDocument.Parse(
                    """{"tool_use_id":"t0","agent":"puls","event":"content_block_start","data":{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t3","name":"NAME","input":INPUT}}}"""
                        .Replace("NAME", name).Replace("INPUT", inputJson))
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
