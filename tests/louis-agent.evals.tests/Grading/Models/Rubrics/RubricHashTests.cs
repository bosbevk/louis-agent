using LouisAgent.Evals.Grading.Models.Rubrics;

namespace LouisAgent.Evals.Tests.Grading.Models.Rubrics;

/// <summary>
/// A rubric's hash names the rubric, not the computer it was read on: the same text hashes the same whether its
/// lines end in LF (a Linux checkout, CI, the containers) or CRLF (a Windows checkout), so a judge's grades carry
/// one hash everywhere and the trend reads the same (ADR 0014: tests are platform-neutral).
/// </summary>
public class RubricHashTests
{
    /// <summary>The same rubric, as git gives it on Windows and on Linux, is one hash.</summary>
    [Test]
    public void TheSameText_HashesTheSame_WhateverItsLineEndings()
    {
        const string lf = "# Judge: is it a meal\n\nSay yes when it is.\n";
        var crlf = lf.Replace("\n", "\r\n");

        Assert.That(RubricHash.Of(crlf), Is.EqualTo(RubricHash.Of(lf)));
        Assert.That(RubricHash.Of(lf), Has.Length.EqualTo(12));
    }

    /// <summary>A changed word is another rubric, so another hash.</summary>
    [Test]
    public void AChangedWord_IsAnotherHash() =>
        Assert.That(RubricHash.Of("Say yes when it is a meal."), Is.Not.EqualTo(RubricHash.Of("Say yes when it is.")));
}
