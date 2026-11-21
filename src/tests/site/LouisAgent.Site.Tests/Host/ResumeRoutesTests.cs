using System.Text;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Endpoints.Routing;
using LouisAgent.Site.Content;
using LouisAgent.Site.Resumes;
using LouisAgent.SiteApi.Resumes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace LouisAgent.Site.Tests.Host;

/// <summary>
/// F142: the site's host serves the CV to anyone and saves it behind the admin key, on routes of its own under
/// <c>/v1</c> with the API's own access marks and error replies (Louis's pick of three: open the API's seam). A save
/// that a public page mustn't show, or that isn't the CV's shape, is refused and nothing is kept.
/// </summary>
public class ResumeRoutesTests
{
    /// <summary>A store on a folder of this test's own, and the folder to delete after.</summary>
    private static (ResumeStore Store, string Folder) NewStore()
    {
        string folder = Directory.CreateTempSubdirectory("louis-agent-cv-").FullName;
        return (new ResumeStore(folder), folder);
    }

    /// <summary>A request carrying this body.</summary>
    private static HttpRequest RequestWith(string body)
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        return context.Request;
    }

    /// <summary>
    /// Anyone may read the CV the page shows. The admin's own route, where the editor reads its copy and saves it,
    /// is behind the admin key both ways (FX153, Louis: "this should be behind the key it is not"): an editor that
    /// read the open route opened for anyone.
    /// </summary>
    [Test]
    public void TheCv_IsOpenToRead_AndTheAdminsRouteIsBehindTheKey_BothWays()
    {
        var (store, folder) = NewStore();
        try
        {
            var app = WebApplication.CreateBuilder().Build();

            // On a /v1 group, as the API's MapLouisAgentHostGroup hands the host one.
            app.MapGroup("/v1").MapSiteResume(store, adminKey: "the-admin-key");

            var routes = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
                .OfType<RouteEndpoint>().ToList();
            var open = routes.Single(route => route.RoutePattern.RawText == "/v1/site/cv");
            var admin = routes.Where(route => route.RoutePattern.RawText == "/v1/admin/cv").ToList();
            Assert.Multiple(() =>
            {
                Assert.That(RouteAccessExtensions.AccessOf(open), Is.EqualTo(RouteAccess.Open));
                Assert.That(admin.SelectMany(route => route.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods)
                    .Order(), Is.EqualTo(new[] { "GET", "PUT" }));
                Assert.That(admin.Select(RouteAccessExtensions.AccessOf), Has.All.EqualTo(RouteAccess.AdminKey));
            });
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>The read answers the CV the store holds.</summary>
    [Test]
    public void TheRead_AnswersTheCv()
    {
        var (store, folder) = NewStore();
        try
        {
            var answer = new ResumeHandlers(store).Get();

            Assert.That(answer.Value!.Basics.Name, Is.EqualTo(Cv.Resume.Basics.Name));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>A good save is kept and answered back.</summary>
    [Test]
    public async Task AGoodSave_IsKept()
    {
        var (store, folder) = NewStore();
        try
        {
            var answer = await new ResumeHandlers(store).PutAsync(
                RequestWith("""{ "basics": { "name": "Louis Marais", "label": "Edited" } }"""));

            Assert.Multiple(() =>
            {
                Assert.That(answer.Result, Is.InstanceOf<JsonHttpResult<Resume>>());
                Assert.That(store.Read().Basics.Label, Is.EqualTo("Edited"));
            });
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>A misspelt field, or something a public page mustn't show, is a 400 and nothing is kept.</summary>
    [TestCase("""{ "basics": { "name": "Louis Marais", "lable": "Edited" } }""")]
    [TestCase("""{ "basics": { "name": "Louis Marais", "summary": "ID 8601015009087" } }""")]
    [TestCase("""{ "basics": { "name": "Louis Marais", "summary": "Salary: R50 000" } }""")]
    [TestCase("{ }")]
    [TestCase("not json")]
    public async Task ABadSave_IsRefused_AndNothingIsKept(string body)
    {
        var (store, folder) = NewStore();
        try
        {
            var answer = await new ResumeHandlers(store).PutAsync(RequestWith(body));

            var refusal = answer.Result as JsonHttpResult<ApiError>;
            Assert.Multiple(() =>
            {
                Assert.That(refusal?.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
                Assert.That(refusal?.Value?.Error.Message, Is.Not.Empty);
                Assert.That(File.Exists(Path.Combine(folder, "cv.json")), Is.False);
            });
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>With nowhere to keep it, a save says so (a 503 naming the setting) and isn't lost silently.</summary>
    [Test]
    public async Task WithNowhereToKeepIt_ASaveSaysSo()
    {
        var answer = await new ResumeHandlers(new ResumeStore(folder: null)).PutAsync(
            RequestWith("""{ "basics": { "name": "Louis Marais" } }"""));

        var refusal = answer.Result as JsonHttpResult<ApiError>;
        Assert.Multiple(() =>
        {
            Assert.That(refusal?.StatusCode, Is.EqualTo(StatusCodes.Status503ServiceUnavailable));
            Assert.That(refusal?.Value?.Error.Message, Does.Contain("SITE_DATA_DIRECTORY"));
        });
    }
}
