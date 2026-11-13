using System.Net;
using System.Net.Http.Json;
using LouisAgent.Site.Content;
using LouisAgent.Site.Resumes;
using LouisAgent.Site.Resumes.Sources;
using LouisAgent.SiteApi.Resumes;

namespace LouisAgent.Site.Tests.Resumes;

/// <summary>
/// F142: where the page gets the CV. In the browser it asks the site's API, so an edit saved in the admin shows; when
/// the API can't answer it shows the CV the site was built with, never an empty page. On the server, where the page
/// is rendered for search engines, the host hands it the store's copy directly.
/// </summary>
public class ResumeSourceTests
{
    /// <summary>A client on the site's address, answered by this.</summary>
    private static HttpClient ClientOver(Func<HttpRequestMessage, HttpResponseMessage> answer) =>
        new(new StubHandler(answer)) { BaseAddress = new Uri("http://site.test/") };

    /// <summary>The API's copy is the one shown.</summary>
    [Test]
    public async Task InTheBrowser_TheCvIsTheApis()
    {
        var saved = Cv.Resume with { Basics = Cv.Resume.Basics with { Label = "Saved in the admin" } };
        var answer = JsonContent.Create(saved, options: ResumeReader.Options);
        var http = ClientOver(request => request.RequestUri!.AbsolutePath == "/v1/site/cv"
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = answer }
            : new HttpResponseMessage(HttpStatusCode.NotFound));

        var resume = await new ApiResumeSource(http).ReadAsync();

        Assert.That(resume.Basics.Label, Is.EqualTo("Saved in the admin"));
    }

    /// <summary>An API that is down, or answers something else, leaves the CV the site was built with.</summary>
    [TestCase(HttpStatusCode.NotFound)]
    [TestCase(HttpStatusCode.InternalServerError)]
    public async Task WhenTheApiCantAnswer_TheCvIsTheEmbeddedOne(HttpStatusCode status)
    {
        var http = ClientOver(_ => new HttpResponseMessage(status));

        var resume = await new ApiResumeSource(http).ReadAsync();

        Assert.That(resume.Basics.Name, Is.EqualTo(Cv.Resume.Basics.Name));
    }

    /// <summary>A client with no address (the server's own) can't ask, and the CV is the embedded one.</summary>
    [Test]
    public async Task AClientWithNoAddress_GivesTheEmbeddedCv()
    {
        using var http = new HttpClient();

        var resume = await new ApiResumeSource(http).ReadAsync();

        Assert.That(resume.Basics.Name, Is.EqualTo(Cv.Resume.Basics.Name));
    }

    /// <summary>On the server, the source is the store itself.</summary>
    [Test]
    public async Task OnTheServer_TheCvIsTheStores()
    {
        string folder = Directory.CreateTempSubdirectory("louis-agent-cv-").FullName;
        try
        {
            var store = new ResumeStore(folder);
            store.Save(Cv.Resume with { Basics = Cv.Resume.Basics with { Label = "Saved in the admin" } });

            var resume = await new StoredResumeSource(store).ReadAsync();

            Assert.That(resume.Basics.Label, Is.EqualTo("Saved in the admin"));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
