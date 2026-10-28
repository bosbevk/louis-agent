using LouisAgent.Puls.Admin.Runs;

namespace LouisAgent.Puls.Tests.Admin.Runs;

/// <summary>
/// How the Benchmarks page writes a run's status, its repeats, its money and a question too long for a cell.
/// </summary>
public class RunTextTests
{
    /// <summary>A run is running or done; anything else has failed.</summary>
    [TestCase("running", "running")]
    [TestCase("done", "done")]
    [TestCase("failed", "failed")]
    [TestCase("killed", "failed")]
    public void AStatus_IsRunningDoneOrFailed(string status, string shown)
    {
        Assert.That(RunText.Status(status), Is.EqualTo(shown));
    }

    /// <summary>One repeat, or more repeats.</summary>
    [Test]
    public void Repeats_AreCountedInWords()
    {
        Assert.That((RunText.Repeats(1), RunText.Repeats(3)), Is.EqualTo(("1 repeat", "3 repeats")));
    }

    /// <summary>A known cost in dollars; an unknown one is a dash, never 0.</summary>
    [Test]
    public void AnUnknownCost_IsADash()
    {
        Assert.That((RunText.Money(0.5m, null), RunText.Money(null, null)), Is.EqualTo(("$0.5", "—")));
    }

    /// <summary>
    /// A question up to 70 characters is shown whole; a longer one is cut at 69 and ends in an ellipsis.
    /// </summary>
    [Test]
    public void ALongQuestion_IsCut()
    {
        string seventy = new('a', 70);

        Assert.That(RunText.Blurb(null), Is.EqualTo(""));
        Assert.That(RunText.Blurb(""), Is.EqualTo(""));
        Assert.That(RunText.Blurb(seventy), Is.EqualTo(seventy));
        Assert.That(RunText.Blurb(new string('a', 68) + "  bcd"), Is.EqualTo(new string('a', 68) + "…"));
    }
}
