using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Packs.WhereToBuy.Checks;
using LouisAgent.SessionStream;
using static LouisAgent.Packs.WhereToBuy.Tests.Checks.ShopTrials;

namespace LouisAgent.Packs.WhereToBuy.Tests.Checks;

/// <summary>
/// The cook prices at no shop: an answer whose list has a column for one fails, naming it (no shop's prices are read,
/// ADR 0069); an estimates list, or an answer with no list, passes.
/// </summary>
public class UntooledShopsTests
{
    /// <summary>A scenario at Malmesbury's PnP.</summary>
    private const string Pnp = """{"town":"Malmesbury","shop":"pnp"}""";

    /// <summary>A list with a column for Checkers, Pick n Pay and Shoprite, as the scorer's case had it.</summary>
    private const string ThreeShops =
        "| Ingredient | For the recipe | To buy | Checkers | Pick n Pay | Shoprite | Best price |\n"
        + "|---|---|---|---|---|---|---|\n"
        + "| Onions | 2 | Onions 1 kg | [R34,99](https://www.checkers.co.za/p/3) | - | - | R34,99 Checkers |\n";

    /// <summary>A list with only a Woolworths column.</summary>
    private const string Woolworths =
        "| Ingredient | For the recipe | To buy | Woolworths |\n|---|---|---|---|\n| Onions | 2 | Onions 1 kg | - |\n";

    /// <summary>An estimates list, with no shop column at all.</summary>
    private const string Estimates =
        "| Ingredient | For the recipe | To buy | Estimated price |\n|---|---|---|---|\n"
        + "| Uie | 2 | onions 1 kg | ± R25 |\n";

    /// <summary>The grade of a one-turn trial whose turn is these events.</summary>
    private static Task<Grade> Grade(params SessionEvent[] events) =>
        new UntooledShops().GradeAsync(Trial(Pnp, Turn(1, events)), CancellationToken.None).AsTask();

    /// <summary>It is a required fixed rule that reads no expected key.</summary>
    [Test]
    public void ItIsARequiredFixedRule_ThatReadsNoKey()
    {
        var grader = new UntooledShops();

        Assert.That((grader.Name, grader.Version, grader.Kind, grader.Required),
            Is.EqualTo(("untooled-shops", 2, CheckKind.FixedRule, true)));
        Assert.That(grader.ExpectedKeys, Is.Empty);
    }

    /// <summary>A list with a column for any shop fails, and names each.</summary>
    [Test]
    public async Task AColumnForAShop_Fails_NamingIt()
    {
        var grade = await Grade(Text(ThreeShops));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Fail, 0d)));
        Assert.That(grade.Reason, Does.Contain("checkers.co.za, pnp.co.za, shoprite.co.za"));
    }

    /// <summary>A column for a shop the old pack could read (Woolworths as a page) fails now too.</summary>
    [Test]
    public async Task AShopTheOldPackRead_FailsToo() =>
        Assert.That((await Grade(Text(Woolworths))).Outcome, Is.EqualTo(GradeOutcome.Fail));

    /// <summary>An estimates list names no shop, so it passes.</summary>
    [Test]
    public async Task AnEstimatesList_Passes() =>
        Assert.That((await Grade(Text(Estimates))).Outcome, Is.EqualTo(GradeOutcome.Pass));

    /// <summary>A follow-up turn's list counts as the first's does.</summary>
    [Test]
    public async Task AFollowUpsList_CountsToo()
    {
        var trial = Trial(Pnp, Turn(1, [Text(Estimates)]), Turn(2, [Text(ThreeShops)]));

        var grade = await new UntooledShops().GradeAsync(trial, CancellationToken.None);

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
    }

    /// <summary>A turn that failed for the infrastructure is Unknown, never a fail.</summary>
    [Test]
    public async Task ATurnThatFailedForTheInfrastructure_IsUnknown()
    {
        var trial = Trial(Pnp, Turn(1, [Text(ThreeShops)], TurnFailure.Timeout));

        var grade = await new UntooledShops().GradeAsync(trial, CancellationToken.None);

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }
}
