using LouisAgent.Api.Contracts.Admin.Evals;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Evals;

namespace LouisAgent.Puls.Tests.Admin.Evals;

/// <summary>
/// How the eval list words an eval: whose it is, its dataset and graders, its modes' rates, and whether it ran.
/// </summary>
public class EvalTextTests
{
    /// <summary>An eval with no run says so, and one with no store to ask doesn't claim either way.</summary>
    [TestCase(null, 0, "runs not known: no store")]
    [TestCase(true, 0, "never run")]
    [TestCase(false, 1, "1 run")]
    [TestCase(false, 3, "3 runs")]
    public void TheRunCount_SaysNeverRun_OrNotKnown(bool? neverRun, int runs, string expected) =>
        Assert.That(EvalText.Runs(neverRun, runs), Is.EqualTo(expected));

    /// <summary>An eval of no identity is the engine's own, as the stub is.</summary>
    [Test]
    public void AnEvalOfNoIdentity_IsTheEnginesOwn()
    {
        Assert.That(EvalText.Whose(null), Is.EqualTo("the engine's own"));
        Assert.That(EvalText.Whose(SampleIdentity.Alpha), Is.EqualTo(SampleIdentity.Alpha));
    }

    /// <summary>A dataset reads as its name and version; none is said, not left blank.</summary>
    [Test]
    public void TheDataset_IsItsNameAndVersion_OrNone()
    {
        Assert.That(EvalText.Dataset(new EvalDatasetRow("meals", 2)), Is.EqualTo("meals v2"));
        Assert.That(EvalText.Dataset(null), Is.EqualTo("none"));
    }

    /// <summary>A grader reads as its name, its version and, in words, what it judges with.</summary>
    [TestCase("FixedRule", "priced v1 · fixed rule")]
    [TestCase("Model", "priced v1 · model")]
    [TestCase("Human", "priced v1 · a person")]
    [TestCase("Oracle", "priced v1 · Oracle")]
    public void AGrader_IsItsNameVersionAndKind(string kind, string expected) =>
        Assert.That(EvalText.Grader(new EvalGraderRow("priced", 1, kind)), Is.EqualTo(expected));

    /// <summary>A list of names is joined with commas; an empty one is a dash.</summary>
    [Test]
    public void AList_IsJoined_OrADash()
    {
        Assert.That(EvalText.Names(["town", "city"]), Is.EqualTo("town, city"));
        Assert.That(EvalText.Names([]), Is.EqualTo("—"));
    }

    /// <summary>A mode's rates read as pass@k and pass^k, a dash where no trial made one.</summary>
    [Test]
    public void AModesRates_ArePassAtKAndPassHatK()
    {
        var mode = new ModeRow("direct", 2, [], null, null, 1.0, 0.5, 2, 0, 0);
        var unknown = new ModeRow("direct", 1, [], null, null, null, null, null, null, null);

        Assert.That(EvalText.Rates(mode), Is.EqualTo("direct pass@2 100% · pass^2 50%"));
        Assert.That(EvalText.Rates(unknown), Is.EqualTo("direct pass@k – · pass^k –"));
    }

    /// <summary>The version is "v" and the number; one we don't know is a question mark.</summary>
    [Test]
    public void TheVersion_IsVAndTheNumber()
    {
        Assert.That(EvalText.Version(3), Is.EqualTo("v3"));
        Assert.That(EvalText.Version(null), Is.EqualTo("v?"));
    }
}
