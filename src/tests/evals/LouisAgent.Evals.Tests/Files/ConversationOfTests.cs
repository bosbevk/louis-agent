using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Transcripts;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Files.Json;

namespace LouisAgent.Evals.Tests.Files;

/// <summary>
/// F126.4: a conversation of a run made back into a conversation of the eval file, to copy into it and change: each
/// turn's ask, its facts' words and its criteria's words, from the first repeat, written by the file's own options so
/// the file's reader reads it back.
/// </summary>
public class ConversationOfTests
{
    /// <summary>A run's conversation gives its asks, its facts' words and its criteria's words, and its page.</summary>
    [Test]
    public void AConversationOfARun_HasItsAsks_ItsFacts_AndItsCriteria()
    {
        var conversation = Conversation.Of(Played(), "/identities");

        Assert.That(conversation.Id, Is.EqualTo("guide-walk"));
        Assert.That(conversation.Page, Is.EqualTo("/identities"));
        Assert.That(conversation.Turns.Select(turn => turn.Ask), Is.EqualTo(new[] { "What's on this page?", "Next." }));
        Assert.That(conversation.Turns[1].Facts.Select(fact => fact.ToString()),
            Is.EqualTo(new[] { "used ShowPage", "under 30s" }));
        Assert.That(conversation.Turns[1].Criteria, Is.EqualTo(new[] { "Lights the area it talks about" }));
    }

    /// <summary>Its JSON, set in a file, reads back with the file's reader into the same conversation.</summary>
    [Test]
    public void ItsJson_ReadsBackWithTheFilesReader()
    {
        var conversation = Conversation.Of(Played(), null);

        var file = EvalFileReader.Read(
            $$"""{"agent":"louis-agent-guide","conversations":[{{conversation.ToJson()}}]}""");

        Assert.That(file.Conversations.Single().ToJson(), Is.EqualTo(conversation.ToJson()));
        Assert.That(conversation.ToJson(), Does.Not.Contain("\"page\""), "with no page, the file writes no page key");
    }

    /// <summary>A played conversation of two repeats, two turns each; the first repeat is the one copied.</summary>
    private static ConversationRun Played()
    {
        var first = new TurnTranscript("What's on this page?", "The Identities page.", [], TimeSpan.FromSeconds(4));
        var second = new TurnTranscript("Next.", "Next up, packs.", [], TimeSpan.FromSeconds(5));
        var turns = new List<TurnRun>
        {
            new(first, [new Mark("no-error", true, "Ended well.")],
                [new Mark("Offers a walkthrough", true, "It does.")],
                null, null),
            new(second, [new Mark("used ShowPage", true, "Called."), new Mark("under 30s", true, "Took 5 s.")],
                [new Mark("Lights the area it talks about", false, "It doesn't say so.")], null, null)
        };
        return new ConversationRun("guide-walk", 2, [new RepeatRun(1, turns), new RepeatRun(2, turns)]);
    }
}
