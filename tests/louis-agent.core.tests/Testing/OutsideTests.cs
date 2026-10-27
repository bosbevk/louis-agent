namespace LouisAgent.Core.Tests.Testing;

// F18 step 7.6: a missing tool skips the test on a developer's machine and fails it where INTEGRATION_REQUIRED is set; the Puls stack's addresses always skip.
public class OutsideTests
{
    [TestCase(true, false, Outside.Outcome.Run)]
    [TestCase(true, true, Outside.Outcome.Run)]
    [TestCase(false, false, Outside.Outcome.Skip)]
    [TestCase(false, true, Outside.Outcome.Fail)]
    public void AToolThatIsThere_Runs_AndOneThatIsNot_SkipsOrFailsWhenRequired(bool available, bool required, Outside.Outcome expected) =>
        Assert.That(Outside.Decide(available, required), Is.EqualTo(expected));

    [TestCase("1", true)]
    [TestCase("true", true)]
    [TestCase("TRUE", true)]
    [TestCase("0", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void TheRequiredSwitch_IsOnForOneOrTrue(string? value, bool expected) => Assert.That(Outside.IsRequired(value), Is.EqualTo(expected));

    [Test]
    public void AMissingToolThatIsRequired_FailsTheTest_NamingTheReasonAndTheSwitch() =>
        Assert.That(() => Outside.Tool(false, "git isn't installed", required: true),
            Throws.InstanceOf<AssertionException>().With.Message.Contains("git isn't installed").And.Message.Contains("INTEGRATION_REQUIRED"));

    [Test]
    public void AMissingToolThatIsNotRequired_SkipsTheTest() =>
        Assert.That(() => Outside.Tool(false, "git isn't installed", required: false), Throws.InstanceOf<IgnoreException>());

    [Test]
    public void AToolThatIsThere_LetsTheTestRun() => Assert.That(() => Outside.Tool(true, "git isn't installed", required: true), Throws.Nothing);

    [Test]
    public void AStackAddressThatIsNotSet_SkipsTheTest_EvenWhereTools_AreRequired()
    {
        const string variable = "LOUIS_TEST_STACK_ADDRESS_NOT_SET";

        Assert.That(() => Outside.Stack(variable, "a browser"), Throws.InstanceOf<IgnoreException>());
    }
}
