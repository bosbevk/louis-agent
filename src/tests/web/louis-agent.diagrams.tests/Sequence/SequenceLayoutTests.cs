using LouisAgent.Diagrams.Sequence;
using LouisAgent.Diagrams.Sequence.Placed;
using LouisAgent.Diagrams.Sequence.Spec;

namespace LouisAgent.Diagrams.Tests.Sequence;

/// <summary>
/// A sequence's layout (CL60.3): the participants stand in a row, each step takes a row under them, and a message's
/// words keep between the two lifelines they join, so a test can hold what a person would check by eye.
/// </summary>
public class SequenceLayoutTests
{
    /// <summary>Three participants, a long call across the middle one, a reply, and a note to oneself.</summary>
    private static readonly SequenceSpec Spec = new(
        [new Participant("P", "Program.cs"), new Participant("S", "IServiceCollection"), new Participant("B", "B")],
        [
            new MessageStep("P", "S", "AddLouisAgent(agent => …)"),
            new MessageStep("P", "B",
                "the lambda: AddAnthropic(), AddWeb(), AddPack<CalculatorPack>(), AddIdentity<PulsIdentity>()"),
            new MessageStep("B", "B", "BuildCatalog() checks every name"),
            new ReplyStep("B", "P", "AgentHostContext")
        ]);

    /// <summary>The heads stand in one row, left to right in the order given, never touching.</summary>
    [Test]
    public void TheHeads_StandInARow_Apart()
    {
        var heads = SequenceLayout.Place(Spec).Heads;

        Assert.Multiple(() =>
        {
            Assert.That(heads.Select(head => head.Participant.Id), Is.EqualTo(new[] { "P", "S", "B" }));
            Assert.That(heads.Select(head => head.Y).Distinct(), Has.Exactly(1).Items);
            for (int at = 1; at < heads.Count; at++)
                Assert.That(heads[at].X, Is.GreaterThan(heads[at - 1].X + heads[at - 1].W), heads[at].Participant.Id);
        });
    }

    /// <summary>
    /// Every message's words sit between the two lifelines it joins, a long one spread over the gaps it crosses.
    /// </summary>
    [Test]
    public void AMessagesWords_KeepBetweenItsLifelines()
    {
        var placed = SequenceLayout.Place(Spec);

        Assert.Multiple(() =>
        {
            foreach (var row in placed.Rows.Where(row => row.FromX != row.ToX))
            {
                var box = SequenceText.WordBox(row);
                Assert.That(box.X, Is.GreaterThan(Math.Min(row.FromX, row.ToX)), row.Lines[0]);
                Assert.That(box.X + box.W, Is.LessThan(Math.Max(row.FromX, row.ToX)), row.Lines[0]);
            }
        });
    }

    /// <summary>A long message breaks over lines of 40 characters at most, its words all kept.</summary>
    [Test]
    public void ALongMessage_WrapsAtForty()
    {
        var lambda = SequenceLayout.Place(Spec).Rows[1];

        Assert.Multiple(() =>
        {
            Assert.That(lambda.Lines, Has.Count.GreaterThan(1));
            Assert.That(lambda.Lines.Max(line => line.Length), Is.LessThanOrEqualTo(40));
            Assert.That(string.Join(' ', lambda.Lines), Does.EndWith("AddIdentity<PulsIdentity>()"));
        });
    }

    /// <summary>The steps run down in order under the heads, each row below the one before.</summary>
    [Test]
    public void TheRows_RunDownInOrder()
    {
        var placed = SequenceLayout.Place(Spec);
        double headsEnd = placed.Heads.Max(head => head.Y + head.H);

        Assert.Multiple(() =>
        {
            Assert.That(placed.Rows.Select(row => row.Step), Is.EqualTo(Spec.Steps));
            Assert.That(placed.Rows[0].Y, Is.GreaterThan(headsEnd));
            for (int at = 1; at < placed.Rows.Count; at++)
                Assert.That(placed.Rows[at].Y, Is.GreaterThanOrEqualTo(placed.Rows[at - 1].Y + placed.Rows[at - 1].H));
            Assert.That(placed.Height, Is.GreaterThan(placed.Rows[^1].Y + placed.Rows[^1].H));
        });
    }

    /// <summary>
    /// A message to oneself turns back on its own lifeline, its words to the right of it and inside the drawing.
    /// </summary>
    [Test]
    public void AMessageToOneself_TurnsBackBesideItsLifeline()
    {
        var placed = SequenceLayout.Place(Spec);
        var self = placed.Rows[2];
        var box = SequenceText.WordBox(self);

        Assert.Multiple(() =>
        {
            Assert.That(self.FromX, Is.EqualTo(self.ToX));
            Assert.That(box.X, Is.GreaterThan(self.FromX));
            Assert.That(box.X + box.W, Is.LessThanOrEqualTo(placed.Width));
        });
    }
}
