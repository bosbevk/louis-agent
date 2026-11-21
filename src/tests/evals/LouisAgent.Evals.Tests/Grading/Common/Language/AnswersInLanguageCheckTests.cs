using System.Text.Json;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Common.Language;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;

namespace LouisAgent.Evals.Tests.Grading.Common.Language;

/// <summary>
/// The <c>answers-in-language</c> check: the first answer is in the language asked for, told by counting its most
/// common small words against a short list per language, with no model. Too few of them to tell is Unknown.
/// </summary>
public class AnswersInLanguageCheckTests
{
    /// <summary>The start of the cook's Afrikaans reference answer, as the cook gave it.</summary>
    private const string Afrikaans =
        "Ek gee dit gou vir my kok; hy's nou-nou terug met 'n antwoord. My kok is terug, hier is wat hy gekry het: " +
        "Oom Boetie se Kerrie Hoender, 'n familieresep met een pot, hoenderstukke, aartappels en wortels. Dit is " +
        "vanaand maklik en goedkoop: die resep is aangepas van 6–8 na 4 mense.";

    /// <summary>The start of the cook's English reference answer, with its Afrikaans hand-back line.</summary>
    private const string English =
        "I'm handing this to my cook; he'll be back shortly with a dinner for two that fits your hour. My kok is " +
        "terug, hier is wat hy gekry het: Easy one-pot yellow chicken curry with potatoes, onion and baby carrots, " +
        "all cooked in one pan in about 40 minutes. It suits a hurried dinner for two tonight.";

    /// <summary>An answer in the language asked for passes, in either language, the reason naming it.</summary>
    [TestCase(Afrikaans, AnswerLanguage.Afrikaans, "Afrikaans")]
    [TestCase(English, AnswerLanguage.English, "English")]
    public async Task AnAnswerInTheLanguageAsked_Passes(string answer, AnswerLanguage language, string named)
    {
        var grade = await GradeAsync(new AnswersInLanguageCheck(language), Trial(Turn(1, [Text(answer)])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Pass, 1.0)));
        Assert.That(grade.Reason, Does.Contain(named));
    }

    /// <summary>An answer in another language fails, naming the language it reads as.</summary>
    [Test]
    public async Task AnAnswerInAnotherLanguage_Fails_NamingIt()
    {
        var grade = await GradeAsync(new AnswersInLanguageCheck(AnswerLanguage.Afrikaans),
            Trial(Turn(1, [Text(English)])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Fail, 0.0)));
        Assert.That(grade.Reason, Does.Contain("English").And.Contain("Afrikaans"));
    }

    /// <summary>
    /// An answer with too few common words to tell, or as many of each language's, is Unknown, never a guess.
    /// </summary>
    [TestCase("Bobotie, R241.")]
    [TestCase("Die rys and the vleis.")]
    public async Task AnAnswerThatCantBeTold_IsUnknown(string answer) =>
        Assert.That(
            (await GradeAsync(new AnswersInLanguageCheck(AnswerLanguage.English), Trial(Turn(1, [Text(answer)]))))
            .Outcome, Is.EqualTo(GradeOutcome.Unknown));

    /// <summary>No turn, or one the infrastructure broke, is Unknown; a turn that streamed an error fails.</summary>
    [Test]
    public async Task ABrokenTurn_IsUnknown_AndAFailedOne_Fails()
    {
        var check = new AnswersInLanguageCheck(AnswerLanguage.English);

        Assert.That((await GradeAsync(check, Trial())).Outcome, Is.EqualTo(GradeOutcome.Unknown));
        Assert.That((await GradeAsync(check, Trial(Turn(1, failure: TurnFailure.Timeout)))).Outcome,
            Is.EqualTo(GradeOutcome.Unknown));
        Assert.That((await GradeAsync(check, Trial(Turn(1, [Text(English), Error()])))).Outcome,
            Is.EqualTo(GradeOutcome.Fail));
    }

    /// <summary>A word on two languages' lists would count for both, so no word is on two.</summary>
    [Test]
    public void NoWord_IsOnTwoLanguagesLists() =>
        Assert.That(LanguageWords.For(AnswerLanguage.Afrikaans).Intersect(LanguageWords.For(AnswerLanguage.English)),
            Is.Empty);

    /// <summary>
    /// The entry builds the check from a file's settings, the language by its code; a language it has no list for is
    /// refused, naming the ones it has.
    /// </summary>
    [Test]
    public void TheEntry_BuildsTheCheck_AndRefusesALanguageWithNoList()
    {
        var built = Build("""{"language": "af", "required": false}""");

        Assert.That(built, Is.TypeOf<AnswersInLanguageCheck>());
        Assert.That((built.Name, built.Version, built.Required), Is.EqualTo(("answers-in-language", 1, false)));
        Assert.That(Assert.Throws<InvalidDataException>(() => Build("""{"language": "fr"}"""))!.Message,
            Does.Contain("fr").And.Contain("af").And.Contain("en"));
    }

    /// <summary>Grades the trial with the check.</summary>
    private static async Task<Grade> GradeAsync(ICheck check, Trial trial) =>
        await check.GradeAsync(trial, CancellationToken.None);

    /// <summary>Builds the check the way an eval file does, by its type.</summary>
    private static ICheck Build(string settings) =>
        new CheckCatalog(GeneralChecks.All).Get("answers-in-language")
            .Create(JsonDocument.Parse(settings).RootElement.Clone(), CheckContext.None);
}
