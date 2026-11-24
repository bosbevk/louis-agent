namespace LouisAgent.Core.Tests.Testing;

/// <summary>
/// How a test asks for the outside (F18 step 7.6): a missing tool skips the test on a developer's machine and fails it
/// where INTEGRATION_REQUIRED is set; a running stack's address and a model's key always skip when they aren't set.
/// </summary>
public class OutsideTests
{
    /// <summary>A variable no machine sets, for the skips.</summary>
    private const string NotSet = "LOUIS_TEST_OUTSIDE_VARIABLE_NOT_SET";

    /// <summary>A tool that is there runs; a missing one skips, or fails where it is required.</summary>
    [TestCase(true, false, Outside.Outcome.Run)]
    [TestCase(true, true, Outside.Outcome.Run)]
    [TestCase(false, false, Outside.Outcome.Skip)]
    [TestCase(false, true, Outside.Outcome.Fail)]
    public void AToolThatIsThere_Runs_AndOneThatIsNot_SkipsOrFailsWhenRequired(bool available, bool required,
        Outside.Outcome expected) =>
        Assert.That(Outside.Decide(available, required), Is.EqualTo(expected));

    /// <summary>The switch is on for 1 or true, in any case, and off for anything else.</summary>
    [TestCase("1", true)]
    [TestCase("true", true)]
    [TestCase("TRUE", true)]
    [TestCase("0", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void TheRequiredSwitch_IsOnForOneOrTrue(string? value, bool expected) =>
        Assert.That(Outside.IsRequired(value), Is.EqualTo(expected));

    /// <summary>A missing tool that CI is meant to have fails, and says why and which switch asked for it.</summary>
    [Test]
    public void AMissingToolThatIsRequired_FailsTheTest_NamingTheReasonAndTheSwitch() =>
        Assert.That(() => Outside.Tool(false, "git isn't installed", required: true),
            Throws.InstanceOf<AssertionException>().With.Message.Contains("git isn't installed").And.Message
                .Contains("INTEGRATION_REQUIRED"));

    /// <summary>A missing tool on a developer's machine only skips.</summary>
    [Test]
    public void AMissingToolThatIsNotRequired_SkipsTheTest() =>
        Assert.That(() => Outside.Tool(false, "git isn't installed", required: false),
            Throws.InstanceOf<IgnoreException>());

    /// <summary>A tool that is there lets the test go on.</summary>
    [Test]
    public void AToolThatIsThere_LetsTheTestRun() =>
        Assert.That(() => Outside.Tool(true, "git isn't installed", required: true), Throws.Nothing);

    /// <summary>A stack's address that isn't set skips, even where every tool is required.</summary>
    [Test]
    public void AStackAddressThatIsNotSet_SkipsTheTest_EvenWhereTools_AreRequired() =>
        Assert.That(() => Outside.Stack(NotSet, "a browser"), Throws.InstanceOf<IgnoreException>());

    /// <summary>A model's key that isn't set skips, and says the key is missing, not a stack (F126.2).</summary>
    [Test]
    public void AKeyThatIsNotSet_SkipsTheTest_SayingTheKeyIsMissing() =>
        Assert.That(() => Outside.Key(NotSet, "the judge's model"),
            Throws.InstanceOf<IgnoreException>().With.Message.Contains(NotSet).And.Message.Contains("key"));

    /// <summary>
    /// A file kept on this machine and never in the repository, such as a real bill (F171.1), skips when its path
    /// isn't set, and says it is a file on this machine.
    /// </summary>
    [Test]
    public void ALocalFileThatIsNotSet_SkipsTheTest_SayingItIsAFileOnThisMachine() =>
        Assert.That(() => Outside.LocalFile(NotSet, "real bill"),
            Throws.InstanceOf<IgnoreException>().With.Message.Contains(NotSet).And.Message.Contains("this machine"));
}
