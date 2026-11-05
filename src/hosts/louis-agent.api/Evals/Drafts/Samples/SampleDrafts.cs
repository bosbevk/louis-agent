using LouisAgent.Composition.Messages;
using System.Text.Json;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Protocol.Parts.Conversations;

namespace LouisAgent.Api.Evals.Drafts.Samples;

/// <summary>
/// Makes a sample draft from a stored run's conversation and the scenario it ran: the first question is the input,
/// the later ones the follow-ups; the place, time zone, expected keys and categories are the scenario's; the person's
/// values go on top.
/// </summary>
/// <remarks>
/// A static factory (ADR 0040's <c>Of</c>): one record built from others, with no state of its own. A sample made from
/// a run is a variant of its scenario, so its expected values keep the scenario's keys and only their values change: a
/// key the scenario doesn't have is refused, since no check would read it (finding O, Louis's pick).
/// </remarks>
public static class SampleDrafts
{
    /// <summary>The draft of a sample made from these turns, over this scenario, with what the person chose.</summary>
    /// <remarks>
    /// Guard clauses: an id that can't name a file, then a conversation with no turns, which has no question to make a
    /// sample of. The refusals' messages are what the person reads, so they carry no parameter name.
    /// </remarks>
    public static SampleDraft Of(string id, string eval, string identity, string run, ConversationKey conversation,
        IReadOnlyList<EvalDocuments.Turn> turns, SampleBody scenario, SampleChoices choices, DateTimeOffset at)
    {
        if (!KebabCase.Is(choices.SampleId))
            throw new ArgumentException(MessageText.Format(DraftMessage.SampleIdNotKebabCase,
                new { Id = choices.SampleId }));
        if (turns.Count == 0)
            throw new ArgumentException(MessageText.Format(DraftMessage.ConversationHasNoTurns,
                new
                {
                    conversation.Mode,
                    conversation.Repeat,
                    conversation.Task
                }));

        // The questions in the order they were asked: the first opens the sample, the rest follow up.
        var questions = turns.OrderBy(t => t.Key.Turn).Select(t => t.Question).ToList();
        var metadata = new SampleBodyMetadata(scenario.Metadata.Place, scenario.Metadata.TimeZone, questions[1..],
            Expected(scenario, choices.Expected), choices.Categories ?? scenario.Metadata.Categories);
        var sample = new SampleBody(choices.SampleId, [new SampleBodyMessage("user", questions[0])], metadata,
            choices.Target);
        return new SampleDraft(id, eval, identity, run, conversation, sample, choices.Author, at);
    }

    /// <summary>
    /// The scenario's expected values with the person's on top, key by key; with none from the person, the scenario's.
    /// </summary>
    /// <remarks>Guard clauses: nothing given, then not an object, then each key the scenario must have.</remarks>
    private static JsonElement Expected(SampleBody scenario, JsonElement? given)
    {
        if (given is not { } changes) return scenario.Metadata.Expected;
        if (changes.ValueKind != JsonValueKind.Object)
            throw new ArgumentException(MessageText.Format(DraftMessage.ExpectedNotAnObject));

        // The scenario's object, each given key replacing its value; JsonObject since the keys are the scenario's own.
        var expected = JsonSerializer.SerializeToNode(scenario.Metadata.Expected)!.AsObject();
        foreach (var change in changes.EnumerateObject())
        {
            if (!expected.ContainsKey(change.Name))
                throw new ArgumentException(MessageText.Format(DraftMessage.ExpectedKeyUnknown,
                    new
                    {
                        Key = change.Name,
                        Scenario = scenario.Id,
                        Keys = string.Join(", ", expected.Select(pair => pair.Key))
                    }));
            expected[change.Name] = JsonSerializer.SerializeToNode(change.Value);
        }

        return JsonSerializer.SerializeToElement(expected);
    }
}
