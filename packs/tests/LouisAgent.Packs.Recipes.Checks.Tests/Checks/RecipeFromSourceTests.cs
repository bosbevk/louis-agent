using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Packs.Recipes.Checks;
using LouisAgent.SessionStream;
using static LouisAgent.Packs.Recipes.Tests.Checks.RecipeTrials;

namespace LouisAgent.Packs.Recipes.Tests.Checks;

/// <summary>
/// The cook's recipe comes from a site in the sites table: the link the reader finds in the first turn's answer is on
/// one of them (the host with or without <c>www.</c>, under the site's home), and the grade says which site, or which
/// host it was instead. No link fails; a turn that broke is Unknown, never a fail.
/// </summary>
public class RecipeFromSourceTests
{
    /// <summary>An answer; the fake reader decides what link it holds, so the words don't matter.</summary>
    private const string Answer = "Resep\n\nLink: somewhere\n";

    /// <summary>The grade of a one-turn trial whose answer the reader finds this link in.</summary>
    private static Task<Grade> Grade(string? link, params SessionEvent[] events) =>
        new RecipeFromSource(new FakeLinks(link))
            .GradeAsync(Trial(Turn(1, events.Length > 0 ? events : [Text(Answer)])), CancellationToken.None).AsTask();

    /// <summary>It is a required fixed rule that reads no expected key.</summary>
    [Test]
    public void ItIsARequiredFixedRule_ThatReadsNoKey()
    {
        var check = new RecipeFromSource(FakeLinks.None);

        Assert.That((check.Name, check.Version, check.Kind, check.Required),
            Is.EqualTo(("recipe-from-source", 2, CheckKind.FixedRule, true)));
        Assert.That(check.ExpectedKeys, Is.Empty);
    }

    /// <summary>A link on a site of the table passes, and the reason names the site's short name.</summary>
    [TestCase("https://www.foodandhome.co.za/recipes/pork-bobotie", "foodandhome")]
    [TestCase("https://foodandhome.co.za/recipes/pork-bobotie", "foodandhome")]
    [TestCase("https://thecinnamonjar.com/easy-south-african-bobotie/", "thecinnamonjar")]
    [TestCase("https://novanews.co.za/rooirose/vinnige-hoenderkerrie/", "rooirose")]
    [TestCase("https://NOVANEWS.co.za/rooirose", "rooirose")]
    public async Task ALinkOnATableSite_Passes_NamingTheSite(string link, string slug)
    {
        var grade = await Grade(link, Text(Answer));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Pass, 1d)));
        Assert.That(grade.Reason, Does.Contain(slug));
    }

    /// <summary>
    /// A dish no table site holds may come from the web, when the answer says so under Unsure: that is the done-when.
    /// </summary>
    [TestCase("### Unsure\n\n| What | Why |\n|---|---|\n| Recipe source | None of the sites had it: from a web search. |\n")]
    [TestCase("**Onseker**\n\n| Wat | Waarom |\n|---|---|\n| Resepbron | Uit 'n websoektog. |\n")]
    public async Task ALinkOnAnotherHost_Passes_WhenTheAnswerSaysItCameFromTheWeb(string unsure)
    {
        var grade = await Grade("https://www.example.com/bobotie", Text(Answer + unsure));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
        Assert.That(grade.Reason, Does.Contain("example.com").And.Contain("from the web"));
    }

    /// <summary>Saying so anywhere but under Unsure doesn't count: that's where the user looks for doubts.</summary>
    [Test]
    public async Task SayingItOutsideUnsure_DoesntCount()
    {
        var grade = await Grade("https://www.example.com/bobotie", Text("I did a web search.\n" + Answer));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
    }

    /// <summary>A link on another host fails, naming the host.</summary>
    [TestCase("https://www.example.com/bobotie", "example.com")]
    [TestCase("https://af.hiloved.com/maklike-kerrie-hoender-resep/", "af.hiloved.com")]
    [TestCase("https://marindakook.co.za/bobotie/", "marindakook.co.za")]
    public async Task ALinkOnAnotherHost_Fails_NamingTheHost(string link, string host)
    {
        var grade = await Grade(link);

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Fail, 0d)));
        Assert.That(grade.Reason, Does.Contain(host));
    }

    /// <summary>
    /// Rooi Rose lives under the publisher's home, so the publisher's other sections, a look-alike path and a host
    /// that only ends the same way are no Rooi Rose.
    /// </summary>
    [TestCase("https://novanews.co.za/nuus/n-storie/")]
    [TestCase("https://novanews.co.za/")]
    [TestCase("https://novanews.co.za/rooirose-extra/dis-nie-dit-nie")]
    [TestCase("https://notnovanews.co.za/rooirose/hoenderkerrie")]
    public async Task APageOutsideRooiRosesHome_IsNoSite(string link)
    {
        var grade = await Grade(link);

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Not.Contain("rooirose"));
    }

    /// <summary>An answer the reader finds no link in fails, and says so.</summary>
    [Test]
    public async Task NoRecipeLink_Fails()
    {
        var grade = await Grade(null);

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Fail, 0d)));
        Assert.That(grade.Reason, Does.Contain("no link"));
    }

    /// <summary>
    /// A first turn that broke is Unknown, never a fail: a stream error, no answer at all, or an infrastructure
    /// failure the harness reports apart.
    /// </summary>
    [Test]
    public async Task ABrokenFirstTurn_IsUnknown()
    {
        var errored = await Grade("https://www.example.com/x", Text(Answer), Error());
        var silent = await Grade(null, Text(""));
        var timedOut = await new RecipeFromSource(new FakeLinks("https://www.example.com/x")).GradeAsync(
            Trial(Turn(1, [Text(Answer)], TurnFailure.Transport)), CancellationToken.None);

        Assert.That(new[] { errored.Outcome, silent.Outcome, timedOut.Outcome },
            Has.All.EqualTo(GradeOutcome.Unknown));
    }

    /// <summary>The link of a later turn doesn't count: only the first turn's recipe is the rule's.</summary>
    [Test]
    public async Task OnlyTheFirstTurnsAnswer_IsRead()
    {
        var grade = await new RecipeFromSource(new FakeLinks("https://www.foodandhome.co.za/recipes/pork-bobotie"))
            .GradeAsync(Trial(Turn(1, [Text("")]), Turn(2, [Text(Answer)])), CancellationToken.None);

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }
}
