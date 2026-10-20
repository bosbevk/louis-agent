using LouisAgent.Web.Git;

namespace LouisAgent.Web.Tests.Git;

// W2b: a commit's date as the panels say it: minutes, hours and days ago up to a month, then the date itself.
public class RelativeTimeTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    [TestCase("2026-10-20T11:59:40Z", "just now")]
    [TestCase("2026-10-20T11:55:00Z", "5m ago")]
    [TestCase("2026-10-20T09:00:00Z", "3h ago")]
    [TestCase("2026-10-18T12:00:00Z", "2d ago")]
    [TestCase("2026-09-10T12:00:00Z", "2026-09-10")]
    public void AnAge_IsSaidInItsLargestUnit(string date, string expected) =>
        Assert.That(RelativeTime.Ago(date, Now), Is.EqualTo(expected));

    [Test]
    public void WhatIsntADate_IsShownAsItIs() =>
        Assert.That(RelativeTime.Ago("gister", Now), Is.EqualTo("gister"));
}
