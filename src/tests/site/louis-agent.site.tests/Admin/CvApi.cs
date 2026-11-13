using System.Net;
using System.Net.Http.Json;
using LouisAgent.Api.Contracts;
using LouisAgent.Site.Resumes;
using LouisAgent.Site.Tests.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Site.Tests.Admin;

/// <summary>
/// The site's API as far as the CV goes, for the admin's tests: it holds one CV, hands it out, and keeps what is
/// saved, or refuses with a reason when told to.
/// </summary>
internal sealed class CvApi(Resume held)
{
    /// <summary>The CV the API holds now.</summary>
    public Resume Held { get; private set; } = held;

    /// <summary>Every body a save was sent with, in order.</summary>
    public List<string> Saves { get; } = [];

    /// <summary>Why the next saves are refused, when they are.</summary>
    public string? Refusal { get; set; }

    /// <summary>The site's page context, its API being this one.</summary>
    public BunitContext Context()
    {
        var ctx = SitePageContext.Create();
        var answers = new StubHandler(Answer);
        ctx.Services.AddSingleton(new HttpClient(answers) { BaseAddress = new Uri("http://site.test/") });
        return ctx;
    }

    /// <summary>The open read, the save behind the key, and nothing else.</summary>
    private HttpResponseMessage Answer(HttpRequestMessage request)
    {
        string path = request.RequestUri!.AbsolutePath;
        if (request.Method == HttpMethod.Get && path == "/v1/site/cv") return Json(HttpStatusCode.OK, Held);
        if (request.Method == HttpMethod.Put && path == "/v1/admin/cv") return Save(request);
        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    /// <summary>Keeps what was sent, as the real API does once it has read it strictly, or refuses.</summary>
    private HttpResponseMessage Save(HttpRequestMessage request)
    {
        string body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
        Saves.Add(body);
        if (Refusal is { } why)
        {
            var detail = new ApiErrorDetail("refused", why);
            var error = new ApiError("refused", detail);
            return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = JsonContent.Create(error) };
        }

        Held = ResumeReader.Read(body);
        return Json(HttpStatusCode.OK, Held);
    }

    /// <summary>A CV as the API writes it.</summary>
    private static HttpResponseMessage Json(HttpStatusCode status, Resume resume) =>
        new(status) { Content = JsonContent.Create(resume, options: ResumeReader.Options) };
}
