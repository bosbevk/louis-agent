using LouisAgent.Composition.Agents;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Files.Json;
using LouisAgent.Evals.Files.Pages;

namespace LouisAgent.Evals.Tests.Files.Json;

/// <summary>
/// The reader reads the guide's example (F126.2 step 5): one agent by its address, seven conversations, each turn's
/// facts read into facts, and a turn that leaves out its facts or criteria has none.
/// </summary>
public class EvalFileReaderTests
{
    /// <summary>The guide's seven conversations, in the shape every eval file has.</summary>
    private static readonly string Guide = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
        "src/tests/evals/LouisAgent.Evals.Tests/Files/Json/Fixtures/guide.eval.json"));

    /// <summary>The file names the guide, by its address.</summary>
    /// <summary>F126.5.1: a conversation may give its page in the file: its address, title and text.</summary>
    [Test]
    public void AGivenPage_IsRead()
    {
        const string json = """
                            { "agent": "acme", "conversations": [ { "id": "one",
                              "givenPage": { "url": "/pricing", "title": "Pricing", "text": "What it costs." },
                              "turns": [ { "ask": "Hi", "facts": ["no-error"] } ] } ] }
                            """;

        var conversation = EvalFileReader.Read(json).Conversations.Single();

        Assert.That(conversation.GivenPage, Is.EqualTo(new GivenPage("/pricing", "What it costs.", "Pricing")));
        Assert.That(conversation.Page, Is.Null);
    }

    [Test]
    public void TheGuidesFile_NamesTheGuide() =>
        Assert.That(EvalFileReader.Read(Guide).Agent, Is.EqualTo(new AgentAddress("louis-agent-guide", null)));

    /// <summary>All seven conversations are read, the walkthrough with its three turns and its page.</summary>
    [Test]
    public void TheGuidesFile_HoldsItsSevenConversations()
    {
        var file = EvalFileReader.Read(Guide);

        var walkthrough = file.Conversations.Single(conversation => conversation.Id == "guide-walkthrough");
        Assert.Multiple(() =>
        {
            Assert.That(file.Conversations, Has.Count.EqualTo(7));
            Assert.That(walkthrough.Turns, Has.Count.EqualTo(3));
            Assert.That(walkthrough.Page, Is.EqualTo("/identities"));
        });
    }

    /// <summary>The first turn's facts are read into facts.</summary>
    [Test]
    public void TheFirstTurnsFacts_AreReadIntoFacts()
    {
        var turn = EvalFileReader.Read(Guide).Conversations[0].Turns[0];

        Assert.That(turn.Facts,
            Is.EqualTo(new[]
            {
                new Fact(FactKind.NoError, ""), new Fact(FactKind.Used, "ReadDoc"), new Fact(FactKind.Under, "30")
            }));
    }

    /// <summary>A turn that leaves out its facts or its criteria has none of them.</summary>
    [Test]
    public void ATurnThatLeavesThemOut_HasNoFactsOrCriteria()
    {
        const string json = """
                            { "agent": "acme", "conversations": [ { "id": "one", "turns": [
                              { "ask": "Hello", "criteria": ["Greets back"] },
                              { "ask": "Next.", "facts": ["no-error"] } ] } ] }
                            """;

        var turns = EvalFileReader.Read(json).Conversations[0].Turns;

        Assert.Multiple(() =>
        {
            Assert.That(turns[0].Facts, Is.Empty);
            Assert.That(turns[1].Criteria, Is.Empty);
        });
    }

    /// <summary>A file with no <c>$schema</c> reads too.</summary>
    [Test]
    public void AFileWithNoSchema_Reads()
    {
        const string json = """
                            { "agent": "acme", "conversations": [
                              { "id": "one", "turns": [ { "ask": "Hi", "facts": ["no-error"] } ] } ] }
                            """;

        Assert.That(EvalFileReader.Read(json).Schema, Is.Null);
    }
}
