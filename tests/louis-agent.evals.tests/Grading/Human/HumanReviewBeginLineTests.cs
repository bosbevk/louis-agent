using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Human;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Tests.Grading.Human;

// F18 step 6a.3 (Louis's pick: "on the run's first line"): the portal asks a person the human review's question, so the run's first line carries it, on the review's entry in `graders`: what
// that run was asked, even if the judge prompt changes later. A code grader's entry has none. A run and a re-score write the same line, since both write the eval's graders.
// Written first: the first line carried a grader's name, version and kind only.
public class HumanReviewBeginLineTests
{
    private const string Question = "Is this a meal for the people asked for?";

    private sealed class Judge : IGrader
    {
        public string Name => "meal-for-people";
        public int Version => 1;
        public GraderKind Kind => GraderKind.Model;
        public bool Required => false;
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class Rule : IGrader
    {
        public string Name => "routed-to";
        public int Version => 1;
        public GraderKind Kind => GraderKind.FixedRule;
        public bool Required => true;
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private static JsonElement[] Graders(params IGrader[] graders)
    {
        var sink = new StringWriter();
        new ProtocolWriter(sink, new FixedClock(DateTimeOffset.UnixEpoch)).Begin(SampleEval.Beta, 9, "abc", graders,
            []);
        using var line = JsonDocument.Parse(sink.ToString().Trim().TrimStart('@'));
        return [.. line.RootElement.GetProperty("graders").EnumerateArray().Select(g => g.Clone())];
    }

    [Test]
    public void AHumanReviewsEntry_CarriesItsQuestion()
    {
        var entries = Graders(new Rule(), new Judge(), HumanReviewGrader.For(new Judge(), Question));

        Assert.That(entries[2].GetProperty("name").GetString(), Is.EqualTo("meal-for-people.human"));
        Assert.That(entries[2].GetProperty("question").GetString(), Is.EqualTo(Question));
    }

    [Test]
    public void ACodeGradersOrAJudgesEntry_HasNoQuestion()
    {
        var entries = Graders(new Rule(), new Judge());

        Assert.That(entries.Any(e => e.TryGetProperty("question", out _)), Is.False,
            "the lines of every other grader are as they were");
    }
}
