using LouisAgent.Site.Resumes;
using LouisAgent.Site.Resumes.Privacy;

namespace LouisAgent.Site.Tests.Resumes;

/// <summary>
/// F142: what a public CV must not hold (POPIA: identity-theft material, and what weakens a pay talk), found in the
/// file's words, so the embedded file's test and a save from the admin's editor refuse the same things.
/// </summary>
public class ResumePrivacyTests
{
    /// <summary>A clean CV has no problem; a phone number is not an ID number.</summary>
    [Test]
    public void ACleanCv_HasNoProblem() =>
        Assert.That(ResumePrivacy.Problems("""{ "basics": { "name": "Ada", "phone": "066 391 2470" } }"""), Is.Empty);

    /// <summary>Each thing a public page mustn't show is found, however it is written.</summary>
    [TestCase("""{ "basics": { "summary": "ID 8601015009087" } }""", ResumePrivacyProblem.IdNumber)]
    [TestCase("""{ "basics": { "summary": "860101 5009 087" } }""", ResumePrivacyProblem.IdNumber)]
    [TestCase("""{ "basics": { "summary": "My ID number is on request" } }""", ResumePrivacyProblem.IdNumber)]
    [TestCase("""{ "work": [ { "summary": "Salary: R50 000" } ] }""", ResumePrivacyProblem.Salary)]
    [TestCase("""{ "work": [ { "summary": "R50 000 per month" } ] }""", ResumePrivacyProblem.Salary)]
    [TestCase("""{ "basics": { "summary": "Date of birth: 1 January 1986" } }""", ResumePrivacyProblem.BirthDate)]
    [TestCase("""{ "basics": { "location": { "address": "1 Long Street" } } }""", ResumePrivacyProblem.Address)]
    public void WhatAPublicPageMustntShow_IsFound(string json, ResumePrivacyProblem problem) =>
        Assert.That(ResumePrivacy.Problems(json), Does.Contain(problem));
}
