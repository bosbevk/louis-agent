using System.Text;
using System.Text.Json;
using LouisAgent.Site.Resumes;

namespace LouisAgent.Site.Tests.Resumes;

/// <summary>
/// F142: Louis's CV is a JSON Resume file (jsonresume.org's open shape) that the site reads once into typed records,
/// for the page to draw and, in F143, the guide to read; a field the records don't know is refused, so a misspelt one
/// never drops out of the page unnoticed (ADR 0049).
/// </summary>
public class ResumeReaderTests
{
    /// <summary>
    /// A small CV in the JSON Resume shape, with the two fields of our own (the facts and the "now" project), in the
    /// file beside this test.
    /// </summary>
    private static Stream Sample() => File.OpenRead(Path.Combine(TestPaths.RepoRoot,
        "src/tests/site/louis-agent.site.tests/Resumes/sample-resume.json"));

    /// <summary>The JSON, as a stream the reader takes.</summary>
    private static Stream StreamOf(string json) => new MemoryStream(Encoding.UTF8.GetBytes(json));

    /// <summary>
    /// Every part the page draws reads into its record: the profiles, a job's highlights, a skill's keywords.
    /// </summary>
    [Test]
    public void AJsonResume_ReadsIntoTheRecords()
    {
        using var sample = Sample();

        var resume = ResumeReader.Read(sample);

        Assert.Multiple(() =>
        {
            Assert.That(resume.Basics.Name, Is.EqualTo("Ada Sample"));
            Assert.That(resume.Basics.Location!.City, Is.EqualTo("Malmesbury"));
            Assert.That(resume.Basics.Profiles.Select(profile => profile.Network),
                Is.EqualTo(new[] { "LinkedIn", "GitHub" }));
            Assert.That(resume.Facts.Single().Value, Is.EqualTo("20 years"));
            Assert.That(resume.Now!.Highlights, Is.EqualTo(new[] { "Evals and a cost ledger" }));
            Assert.That(resume.Work.Single().Highlights, Is.EqualTo(new[] { "Shipped the price-rule agent" }));
            Assert.That(resume.Work.Single().StartDate, Is.EqualTo("2021-03"));
            Assert.That(resume.Education.Single().StudyType, Is.EqualTo("Diploma"));
            Assert.That(resume.Skills.Single().Keywords, Is.EqualTo(new[] { "Evals", "Tool design" }));
            Assert.That(resume.Certificates.Single().Issuer, Is.EqualTo("Microsoft"));
            Assert.That(resume.Languages.Single().Fluency, Is.EqualTo("Native"));
            Assert.That(resume.Interests.Single().Name, Is.EqualTo("Chess"));
        });
    }

    /// <summary>A part the file leaves out reads as empty, so a short CV still draws.</summary>
    [Test]
    public void APartLeftOut_ReadsAsEmpty()
    {
        var resume = ResumeReader.Read(StreamOf("""{ "basics": { "name": "Ada Sample" } }"""));

        Assert.Multiple(() =>
        {
            Assert.That(resume.Work, Is.Empty);
            Assert.That(resume.Basics.Profiles, Is.Empty);
            Assert.That(resume.Now, Is.Null);
        });
    }

    /// <summary>A misspelt field is refused, not dropped (ADR 0049: no lenient reader).</summary>
    [Test]
    public void AMisspeltField_IsRefused() =>
        Assert.Throws<JsonException>(() =>
            ResumeReader.Read(StreamOf("""{ "basics": { "name": "Ada Sample", "lable": "Developer" } }""")));
}
