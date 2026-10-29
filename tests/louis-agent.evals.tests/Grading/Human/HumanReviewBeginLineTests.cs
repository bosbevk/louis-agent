using LouisAgent.Testing;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Human;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Run;
using LouisAgent.Evals.Tests.Protocol;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Tests.Grading.Human;

/// <summary>
/// The portal asks a person the human review's question, so the run's first line carries it on the review's grader
/// entry: what that run was asked, even if the judge prompt changes later. A code grader's entry has none. A run and a
/// re-score write the same line, since both write the eval's graders.
/// </summary>
public class HumanReviewBeginLineTests
{
    /// <summary>The question the review asks.</summary>
    private const string Question = "Is this a meal for the people asked for?";

    /// <summary>A model grader, the one the review reviews.</summary>
    private sealed class Judge : IGrader
    {
        /// <inheritdoc />
        public string Name => "meal-for-people";

        /// <inheritdoc />
        public int Version => 1;

        /// <inheritdoc />
        public GraderKind Kind => GraderKind.Model;

        /// <inheritdoc />
        public bool Required => false;

        /// <inheritdoc />
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

        /// <summary>Never called: these tests only write the first line.</summary>
        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    /// <summary>A fixed rule.</summary>
    private sealed class Rule : IGrader
    {
        /// <inheritdoc />
        public string Name => "routed-to";

        /// <inheritdoc />
        public int Version => 1;

        /// <inheritdoc />
        public GraderKind Kind => GraderKind.FixedRule;

        /// <inheritdoc />
        public bool Required => true;

        /// <inheritdoc />
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

        /// <summary>Never called: these tests only write the first line.</summary>
        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    /// <summary>The grader entries of the first line a run of these graders writes.</summary>
    private static IReadOnlyList<GraderInfo> Graders(params IGrader[] graders)
    {
        var sink = new StringWriter();

        new ProtocolWriter(sink, new FixedClock(DateTimeOffset.UnixEpoch)).Begin(SampleEval.Beta, 9, "abc", graders,
            []);

        return Written.Events(sink).OfType<ProtocolEvent>().Single().Graders;
    }

    /// <summary>A human review's entry carries the question it asks.</summary>
    [Test]
    public void AHumanReviewsEntry_CarriesItsQuestion()
    {
        var entries = Graders(new Rule(), new Judge(), HumanReviewGrader.For(new Judge(), Question));

        Assert.That((entries[2].Name, entries[2].Question), Is.EqualTo(("meal-for-people.human", Question)));
    }

    /// <summary>A code grader's or a judge's entry has no question.</summary>
    [Test]
    public void ACodeGradersOrAJudgesEntry_HasNoQuestion()
    {
        var entries = Graders(new Rule(), new Judge());

        Assert.That(entries.Select(e => e.Question), Has.All.Null, "the entries of every other grader have none");
    }
}
