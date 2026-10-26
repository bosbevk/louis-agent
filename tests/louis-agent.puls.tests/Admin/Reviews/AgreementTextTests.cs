using LouisAgent.Puls.Admin.Reviews;

namespace LouisAgent.Puls.Tests.Admin.Reviews;

// F18 step 6a.4c (Louis: "I do not understand what I'm reading"): each class of the people's grades reads as one plain sentence, what you did and what the judge did with the same
// answers, and the share beside its bar is short. Written first: the card showed counts in a row.
public class AgreementTextTests
{
    private static ClassAgreement Passed(int pass, int fail, int unknown) => new(pass, fail, unknown, PersonPassed: true);
    private static ClassAgreement Failed(int pass, int fail, int unknown) => new(pass, fail, unknown, PersonPassed: false);

    [Test]
    public void WhatYouPassed_ReadsAsASentence_WithWhatTheJudgeDidToThem()
    {
        Assert.That(AgreementText.Sentence(Passed(3, 0, 0)), Is.EqualTo("You passed 3 answers. The judge passed all 3."));
        Assert.That(AgreementText.Sentence(Passed(13, 1, 0)), Is.EqualTo("You passed 14 answers. The judge passed 13 of them."));
        Assert.That(AgreementText.Sentence(Passed(1, 0, 0)), Is.EqualTo("You passed 1 answer. The judge passed it too."));
        Assert.That(AgreementText.Sentence(Passed(0, 1, 0)), Is.EqualTo("You passed 1 answer. The judge did not."));
        Assert.That(AgreementText.Sentence(Passed(0, 2, 1)), Is.EqualTo("You passed 3 answers. The judge passed none of them."));
        Assert.That(AgreementText.Sentence(Passed(0, 0, 0)), Is.EqualTo("You have passed no answers yet."));
    }

    [Test]
    public void WhatYouFailed_ReadsTheSameWay()
    {
        Assert.That(AgreementText.Sentence(Failed(0, 4, 0)), Is.EqualTo("You failed 4 answers. The judge failed all 4."));
        Assert.That(AgreementText.Sentence(Failed(0, 1, 0)), Is.EqualTo("You failed 1 answer. The judge failed it too."));
        Assert.That(AgreementText.Sentence(Failed(1, 2, 1)), Is.EqualTo("You failed 4 answers. The judge failed 2 of them."));
        Assert.That(AgreementText.Sentence(Failed(0, 0, 0)), Is.EqualTo("You have failed no answers yet."));
    }

    [Test]
    public void TheShareBesideTheBar_IsShort()
    {
        Assert.That(AgreementText.Share(Passed(13, 1, 0)), Is.EqualTo("93%"));
        Assert.That(AgreementText.Share(Passed(3, 0, 0)), Is.EqualTo("100% · few labels"));
        Assert.That(AgreementText.Share(Passed(0, 0, 0)), Is.EqualTo("no labels"));
    }
}
