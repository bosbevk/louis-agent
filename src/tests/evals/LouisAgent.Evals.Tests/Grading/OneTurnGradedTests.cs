using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Transcripts;
using LouisAgent.Evals.Files.Json;
using LouisAgent.Evals.Grading;
using Microsoft.Extensions.AI;

namespace LouisAgent.Evals.Tests.Grading;

/// <summary>
/// One turn graded whole (F126.2 step 11): the guide's file is read, its first conversation's turn is written down by
/// hand as the guide might have answered, code tells its facts, and the judge marks its criteria on a fake model.
/// </summary>
public class OneTurnGradedTests
{
    /// <summary>The guide's turn, as it might have gone: a read of the overview, and an answer that names it.</summary>
    internal static readonly TurnTranscript GuideWhatIs = new(
        "What is louis-agent, in one minute?",
        "louis-agent is an AI agent framework in C#. I read it in docs/README.md. Want to see the architecture page?",
        [
            new ToolCallTranscript("ReadDoc", """{"path":"docs/README.md"}""",
                "# louis-agent\nAn AI agent framework in C#.")
        ],
        TimeSpan.FromSeconds(11));

    /// <summary>Every fact and every criterion of the turn ends as a mark, all met.</summary>
    [Test]
    public async Task TheGuidesFirstTurn_GetsAMarkForEachFactAndCriterion_AllMet()
    {
        string json = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot,
            "src/tests/evals/LouisAgent.Evals.Tests/Files/Json/Fixtures/guide.eval.json"));
        var turn = EvalFileReader.Read(json).Conversations.Single(c => c.Id == "guide-what-is").Turns[0];
        var judge = new Judge(new FakeChatClient(Met, Met, Met));

        var facts = turn.Facts.Select(fact => Facts.Tell(fact, GuideWhatIs, [])).ToList();
        var criteria = new List<Mark>();
        foreach (string criterion in turn.Criteria)
            criteria.Add((await judge.MarkAsync(criterion, [GuideWhatIs])).Mark);

        Assert.Multiple(() =>
        {
            Assert.That(facts.Select(mark => mark.Text), Is.EqualTo(new[] { "no-error", "used ReadDoc", "under 30s" }));
            Assert.That(criteria.Select(mark => mark.Text), Is.EqualTo(turn.Criteria));
            Assert.That(facts.Concat(criteria), Has.All.Matches<Mark>(mark => mark.Met));
        });
    }

    /// <summary>A fake model's answer: met, with a reason.</summary>
    private static ChatResponse Met(IList<ChatMessage> _) =>
        new(new ChatMessage(ChatRole.Assistant,
            """{"reason":"The answer does what the criterion asks.","met":true}"""));
}
