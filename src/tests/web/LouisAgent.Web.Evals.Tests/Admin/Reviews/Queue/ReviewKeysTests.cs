using LouisAgent.Web.Evals.Admin.Reviews.Queue;

namespace LouisAgent.Web.Evals.Tests.Admin.Reviews.Queue;

/// <summary>The Review page's keys (F53.3 step 3): 1 is pass, 2 fail, 3 unsure; any other key is nothing.</summary>
public class ReviewKeysTests
{
    [TestCase("1", "pass")]
    [TestCase("2", "fail")]
    [TestCase("3", "unsure")]
    [TestCase("4", null)]
    [TestCase("Enter", null)]
    public void AKey_IsAVerdict_OrNothing(string key, string? verdict) =>
        Assert.That(ReviewKeys.VerdictOf(key), Is.EqualTo(verdict));
}
