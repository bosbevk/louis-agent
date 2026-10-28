using System.Text.Json;
using LouisAgent.Puls.Admin.Explorer.Tree;
using LouisAgent.Puls.Admin.Prices;
using LouisAgent.Puls.Admin.Scoring;

namespace LouisAgent.Puls.Tests.Admin.Scoring;

/// <summary>
/// What the explorer reads from a conversation's scores: routed right, a real answer, its chips, and per mode the
/// ingredients priced right and the meals without a list.
/// </summary>
public class ExplorerScoresTests
{
    /// <summary>Routed right only when the scores say true; no scores read as not routed right.</summary>
    [TestCase("""{"routed_right":true}""", true)]
    [TestCase("""{"routed_right":false}""", false)]
    [TestCase("""{}""", false)]
    [TestCase(null, false)]
    public void RoutedRight_IsWhatTheScoresSay(string? scores, bool right) =>
        Assert.That(ExplorerScores.RoutedRight(Conversation("chat", scores)), Is.EqualTo(right));

    /// <summary>A mode counts its conversations routed right.</summary>
    [Test]
    public void AMode_CountsItsConversationsRoutedRight() =>
        Assert.That(ExplorerScores.RoutedRight(Mode(Conversation("a", """{"routed_right":true}"""),
                Conversation("b", """{"routed_right":false}"""), Conversation("c", """{"routed_right":true}"""))),
            Is.EqualTo(2));

    /// <summary>
    /// An answer is routed right and, where the check lists ingredients, 80% priced; a cook with no list has none.
    /// </summary>
    [TestCase("chat", """{"routed_right":false}""", false)]
    [TestCase("chat", """{"routed_right":true}""", true)]
    [TestCase("cook-a", """{"routed_right":true,"ingredients":5,"priced_ratio":0.8}""", true)]
    [TestCase("cook-a", """{"routed_right":true,"ingredients":5,"priced_ratio":0.6}""", false)]
    [TestCase("cook-a", """{"routed_right":true,"ingredients":null}""", false)]
    [TestCase("chat", """{"routed_right":true,"ingredients":null}""", true)]
    public void AnAnswer_IsRoutedRightAndPriced(string task, string scores, bool answered) =>
        Assert.That(ExplorerScores.Answered(Conversation(task, scores)), Is.EqualTo(answered));

    /// <summary>The chips are the scores' own, then the errors; scores that aren't an object have none.</summary>
    [Test]
    public void TheChips_AreTheScoresOwn_ThenTheErrors()
    {
        var c = Conversation("chat", """{"routed_right":true,"shop_hosts":["a"],"errors":4}""", errors: 2);

        Assert.That(ExplorerScores.Chips(c),
            Is.EqualTo(new[] { new PricedChip($"{SampleRole.Alpha} ✓", "ok"), new PricedChip("errors 2", "bad") }));
        Assert.That(ExplorerScores.Chips(Conversation("chat", null, errors: 2)), Is.Empty);
    }

    /// <summary>A mode's accuracy adds up its listed ingredients; with none listed it has no accuracy.</summary>
    [Test]
    public void AModesAccuracy_AddsUpItsIngredients()
    {
        var mode = Mode(
            Conversation("cook-a", """{"ingredients":5,"priced":3,"no_shop_rows":1,"unsourced":1,"estimated":0}"""),
            Conversation("cook-b", """{"ingredients":5,"priced":2,"estimated":2}"""),
            Conversation("chat", """{"routed_right":true}"""));

        Assert.That(ExplorerScores.Accuracy(mode), Is.EqualTo(AccuracyRing.Of(5, 1, 1, 2, 10)));
        Assert.That(ExplorerScores.Accuracy(Mode(Conversation("chat", "{}"))), Is.Null);
    }

    /// <summary>A cook that came back without a list counts as unlisted; another task never does.</summary>
    [Test]
    public void AMealWithoutAList_IsCounted() =>
        Assert.That(ExplorerScores.Unlisted(Mode(Conversation("cook-a", """{"ingredients":null}"""),
                Conversation("chat", """{"ingredients":null}"""), Conversation("cook-b", """{"ingredients":3}"""))),
            Is.EqualTo(1));

    /// <summary>A conversation of a task, handled by the sample role, with these scores (none when null).</summary>
    private static ConversationNode Conversation(string task, string? scores, int errors = 0) => new()
    {
        Task = task,
        Role = SampleRole.Alpha,
        Scores = scores is null ? default : JsonDocument.Parse(scores).RootElement.Clone(),
        Errors = errors
    };

    /// <summary>A mode with these conversations.</summary>
    private static ModeNode Mode(params ConversationNode[] conversations)
    {
        var mode = new ModeNode { Name = SampleMode.Alpha };
        mode.Conversations.AddRange(conversations);
        return mode;
    }
}
