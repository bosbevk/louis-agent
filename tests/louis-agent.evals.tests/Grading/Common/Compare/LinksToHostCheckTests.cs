using System.Text.Json;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Common.Compare;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;

namespace LouisAgent.Evals.Tests.Grading.Common.Compare;

/// <summary>
/// The <c>links-to-host</c> check: the first answer links to at least one of the hosts, or a subdomain of one, the
/// way a price is sourced to its shop.
/// </summary>
public class LinksToHostCheckTests
{
    /// <summary>The check over two shops' hosts.</summary>
    private static readonly LinksToHostCheck Check = new(["checkers.co.za", "woolworths.co.za"]);

    /// <summary>A link to one of the hosts passes, whichever way it's written, and the reason names the host.</summary>
    [TestCase("Rys [R20](https://www.checkers.co.za/rice-1kg).")]
    [TestCase("See https://shop.Woolworths.co.za/rice, it's cheaper.")]
    [TestCase("[1]: https://checkers.co.za/rice")]
    public async Task ALinkToAHost_Passes(string answer)
    {
        var grade = await GradeAsync(Check, Trial(Turn(1, [Text(answer)])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Pass, 1.0)));
        Assert.That(grade.Reason, Does.Contain(".co.za"));
    }

    /// <summary>
    /// No link to the hosts fails: a host named in plain words is no link, and a host that only ends the same way is
    /// another site.
    /// </summary>
    [TestCase("Rice is cheapest at checkers.co.za.")]
    [TestCase("[Rice](https://notcheckers.co.za/rice)")]
    [TestCase("[Rice](https://pnp.co.za/rice)")]
    public async Task NoLinkToTheHosts_Fails(string answer)
    {
        var grade = await GradeAsync(Check, Trial(Turn(1, [Text(answer)])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Fail, 0.0)));
        Assert.That(grade.Reason, Does.Contain("checkers.co.za").And.Contain("woolworths.co.za"));
    }

    /// <summary>
    /// No turn, or one the infrastructure broke, is Unknown; a turn that streamed an error fails.
    /// </summary>
    [Test]
    public async Task ABrokenTurn_IsUnknown_AndAFailedOne_Fails()
    {
        Assert.That((await GradeAsync(Check, Trial())).Outcome, Is.EqualTo(GradeOutcome.Unknown));
        Assert.That((await GradeAsync(Check, Trial(Turn(1, failure: TurnFailure.Timeout)))).Outcome,
            Is.EqualTo(GradeOutcome.Unknown));
        Assert.That((await GradeAsync(Check, Trial(Turn(1, [Text("https://checkers.co.za"), Error()])))).Outcome,
            Is.EqualTo(GradeOutcome.Fail));
    }

    /// <summary>The entry builds the check from a file's settings, and refuses an empty list of hosts.</summary>
    [Test]
    public void TheEntry_BuildsTheCheck_AndRefusesNoHosts()
    {
        var built = Build("""{"hosts": ["checkers.co.za"], "required": false}""");

        Assert.That(built, Is.TypeOf<LinksToHostCheck>());
        Assert.That((built.Name, built.Version, built.Required), Is.EqualTo(("links-to-host", 1, false)));
        Assert.That(Assert.Throws<InvalidDataException>(() => Build("""{"hosts": []}"""))!.Message,
            Does.Contain("hosts"));
    }

    /// <summary>Grades the trial with the check.</summary>
    private static async Task<Grade> GradeAsync(ICheck check, Trial trial) =>
        await check.GradeAsync(trial, CancellationToken.None);

    /// <summary>Builds the check the way an eval file does, by its type.</summary>
    private static ICheck Build(string settings) =>
        new CheckCatalog(GeneralChecks.All).Get("links-to-host")
            .Create(JsonDocument.Parse(settings).RootElement.Clone(), CheckContext.None);
}
