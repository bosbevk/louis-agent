using LouisAgent.Evals.Files.Json;

namespace LouisAgent.Evals.Tests.Files.Json;

/// <summary>
/// A wrong eval file is refused once, with its place and its reason (F126.2 step 6, Louis's pick, "Stop at the first
/// fault"): a fault of shape from the serializer, with the line, and a fault of meaning from the reader's own checks.
/// </summary>
public class EvalFileFaultsTests
{
    /// <summary>A turn that checks something, for the files whose fault is elsewhere.</summary>
    private const string Turn = """{ "ask": "Hi", "facts": ["no-error"] }""";

    /// <summary>A whole file around one conversation's text.</summary>
    private static string FileWith(string conversations) =>
        $$"""{ "agent": "acme", "conversations": [ {{conversations}} ] }""";

    /// <summary>A conversation with the given id and turns.</summary>
    private static string Conversation(string id, string turns, string more = "") =>
        $$"""{ "id": "{{id}}"{{more}}, "turns": [ {{turns}} ] }""";

    /// <summary>Each fault, with the place it is refused at and a word of its reason.</summary>
    private static IEnumerable<TestCaseData> Faults()
    {
        yield return new TestCaseData("{ not json", "$", "").SetName("Text that is no JSON");
        yield return new TestCaseData(FileWith(Conversation("one", Turn, ", \"pagee\": \"/\"")),
            "$.conversations[0].pagee", "pagee").SetName("A misspelt field");
        yield return new TestCaseData(FileWith(Conversation("one", """{ "facts": ["no-error"] }""")),
            "$.conversations[0].turns[0]", "ask").SetName("A turn with no ask");
        const string wrongFact = """{ "ask": "Hi", "facts": ["no-error", "notused ReadDoc"] }""";
        yield return new TestCaseData(FileWith(Conversation("one", wrongFact)),
            "$.conversations[0].turns[0].facts[1]", "'notused ReadDoc' is no fact").SetName("A wrong fact");
        yield return new TestCaseData("""{ "agent": "Not An Address", "conversations": [] }""",
            "$.agent", "no agent's address").SetName("An agent that is no address");
        yield return new TestCaseData("""{ "agent": "acme", "conversations": [] }""",
            "$.conversations", "at least one conversation").SetName("No conversations");
        yield return new TestCaseData(FileWith(Conversation("one", Turn) + ", " + Conversation("one", Turn)),
            "$.conversations[1].id", "'one' is used twice").SetName("An id used twice");
        yield return new TestCaseData(FileWith(Conversation(" ", Turn)),
            "$.conversations[0].id", "needs an id").SetName("An empty id");
        yield return new TestCaseData(FileWith(Conversation("one", "")),
            "$.conversations[0].turns", "at least one turn").SetName("A conversation with no turns");
        yield return new TestCaseData(FileWith(Conversation("one", """{ "ask": " ", "facts": ["no-error"] }""")),
            "$.conversations[0].turns[0].ask", "asks nothing").SetName("An empty ask");
        yield return new TestCaseData(FileWith(Conversation("one", """{ "ask": "Hi", "criteria": [""] }""")),
            "$.conversations[0].turns[0].criteria[0]", "empty").SetName("An empty criterion");
        yield return new TestCaseData(FileWith(Conversation("one", Turn, ", \"page\": \"identities\"")),
            "$.conversations[0].page", "'identities'").SetName("A page that doesn't start with a slash");
        yield return new TestCaseData(FileWith(Conversation("one", """{ "ask": "Hi" }""")),
            "$.conversations[0]", "checks nothing").SetName("A conversation that checks nothing");
        const string given = """, "givenPage": { "url": "/pricing", "text": "Prices." }""";
        yield return new TestCaseData(FileWith(Conversation("one", Turn, ", \"page\": \"/\"" + given)),
            "$.conversations[0].givenPage", "not both").SetName("A page named and given");
        const string notAnAddress = """, "givenPage": { "url": "pricing", "text": "Prices." }""";
        yield return new TestCaseData(FileWith(Conversation("one", Turn, notAnAddress)),
            "$.conversations[0].givenPage.url", "'pricing'").SetName("A given page that isn't an address");
    }

    /// <summary>Each fault is refused at its place, with its reason.</summary>
    [TestCaseSource(nameof(Faults))]
    public void AFault_IsRefused_AtItsPlace_WithItsReason(string json, string place, string reason)
    {
        var refused = Assert.Throws<EvalFileException>(() => EvalFileReader.Read(json));

        Assert.Multiple(() =>
        {
            Assert.That(refused!.Place, Is.EqualTo(place));
            Assert.That(refused.Message, Does.StartWith(place).And.Contain(reason));
        });
    }

    /// <summary>A fault of shape says the line it is on.</summary>
    [Test]
    public void AFaultOfShape_SaysItsLine()
    {
        string json = "{\n  \"agent\": \"acme\",\n  \"conversations\": [\n    { \"id\": \"one\", \"turns\": [\n" +
                      "      { \"ask\": \"Hi\", \"facts\": [\"no-error\", \"notused ReadDoc\"] } ] } ] }";

        var refused = Assert.Throws<EvalFileException>(() => EvalFileReader.Read(json));

        Assert.That(refused!.Message, Does.StartWith("$.conversations[0].turns[0].facts[1] (line 5): "));
    }

    /// <summary>A file with two faults is refused at the first, and says nothing of the second.</summary>
    [Test]
    public void AFileWithTwoFaults_IsRefusedAtTheFirst()
    {
        string json = FileWith(Conversation("one", """{ "ask": " " }""") + ", " + Conversation("one", Turn));

        var refused = Assert.Throws<EvalFileException>(() => EvalFileReader.Read(json));

        Assert.Multiple(() =>
        {
            Assert.That(refused!.Place, Is.EqualTo("$.conversations[0].turns[0].ask"));
            Assert.That(refused.Message, Does.Not.Contain("twice"));
        });
    }
}
