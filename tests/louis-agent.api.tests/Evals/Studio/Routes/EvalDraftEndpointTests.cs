using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Api.Evals.Drafts.Store;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Evals.Studio.Routes;

/// <summary>
/// The compose page's drafts (F49.5): an eval being composed is made, replaced, read, listed and deleted. Its name and
/// identity are read from its file, which must be a JSON object naming both in kebab-case; a rubric is a markdown file
/// in kebab-case; who made it is said. A draft that isn't there is a 404.
/// </summary>
public class EvalDraftEndpointTests
{
    private const string AdminKey = "admin-secret";
    private const string Drafts = "/v1/admin/evals/drafts";

    /// <summary>An eval's file as the person typed it: indented, its name and identity in it.</summary>
    private const string File = """
                                {
                                  "name": "weather-eval",
                                  "identity": "weather-agent",
                                  "proves": "The agent says what the weather does"
                                }
                                """;

    /// <summary>The running API with its client and the draft store.</summary>
    private sealed record Host(WebApplication App, HttpClient Client, InMemoryEvalDraftStore Store) : IAsyncDisposable
    {
        /// <summary>Stops the API when the test is done.</summary>
        public async ValueTask DisposeAsync() => await App.DisposeAsync();
    }

    /// <summary>Starts the API with an empty draft store.</summary>
    private static async Task<Host> StartAsync()
    {
        var store = new InMemoryEvalDraftStore();
        var app = ApiTestHost.Build(adminKey: AdminKey, drafts: store);
        await app.StartAsync();
        return new Host(app, app.GetTestClient(), store);
    }

    /// <summary>Sends a request with the admin key, and any body as JSON.</summary>
    private static Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string url,
        object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("x-api-key", AdminKey);
        if (body is not null) request.Content = JsonContent.Create(body);
        return client.SendAsync(request);
    }

    /// <summary>A draft's body as the page sends it; each test changes only the part it is about.</summary>
    private static object Body(string json = File, string rubric = "weather-right.md", string author = "Louis") =>
        new
        {
            json,
            rubrics = new[]
            {
                new
                {
                    name = rubric,
                    text = "Is die weer reg?"
                }
            },
            author
        };

    /// <summary>Reads an answer's body as JSON.</summary>
    private static async Task<JsonElement> JsonOf(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

    /// <summary>A draft is made: 201, its name and identity read from its file, the file kept as typed.</summary>
    [Test]
    public async Task ADraft_IsMade_ItsNameAndIdentityReadFromItsFile()
    {
        await using var host = await StartAsync();

        var response = await Send(host.Client, HttpMethod.Post, Drafts, Body());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created),
            await response.Content.ReadAsStringAsync());
        var row = await JsonOf(response);
        Assert.That((row.GetProperty("name").GetString(), row.GetProperty("identity").GetString(),
                row.GetProperty("json").GetString(), row.GetProperty("rubrics")[0].GetProperty("name").GetString()),
            Is.EqualTo(("weather-eval", "weather-agent", File, "weather-right.md")));
        Assert.That(response.Headers.Location?.ToString(),
            Is.EqualTo($"{Drafts}/{row.GetProperty("id").GetString()}"));
    }

    /// <summary>
    /// A file that isn't a JSON object, or doesn't name its eval and identity in kebab-case, a rubric that isn't a
    /// kebab-case markdown file, or no author, is a 400.
    /// </summary>
    [TestCase("[1]", "weather-right.md", "Louis")]
    [TestCase("{ not json", "weather-right.md", "Louis")]
    [TestCase("""{"identity":"weather-agent"}""", "weather-right.md", "Louis")]
    [TestCase("""{"name":"Weather Eval","identity":"weather-agent"}""", "weather-right.md", "Louis")]
    [TestCase("""{"name":"weather-eval"}""", "weather-right.md", "Louis")]
    [TestCase(File, "../weather.md", "Louis")]
    [TestCase(File, "weather.txt", "Louis")]
    [TestCase(File, "weather-right.md", " ")]
    public async Task ABadDraft_IsA400(string json, string rubric, string author)
    {
        await using var host = await StartAsync();

        var response = await Send(host.Client, HttpMethod.Post, Drafts, Body(json, rubric, author));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>A draft is replaced under its id, read and listed; one that isn't there is a 404.</summary>
    [Test]
    public async Task ADraft_IsReplaced_Read_AndListed()
    {
        await using var host = await StartAsync();
        string id = (await JsonOf(await Send(host.Client, HttpMethod.Post, Drafts, Body())))
            .GetProperty("id").GetString()!;
        string changed = File.Replace("weather does", "weather does tomorrow");

        var replaced = await Send(host.Client, HttpMethod.Put, $"{Drafts}/{id}", Body(changed));
        var read = await JsonOf(await Send(host.Client, HttpMethod.Get, $"{Drafts}/{id}"));
        var listed = await JsonOf(await Send(host.Client, HttpMethod.Get, Drafts));
        var missing = await Send(host.Client, HttpMethod.Put, $"{Drafts}/nope", Body());

        Assert.That(replaced.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(read.GetProperty("json").GetString(), Is.EqualTo(changed));
        Assert.That(listed.GetProperty("drafts").EnumerateArray().Select(d => d.GetProperty("id").GetString()),
            Is.EqualTo(new[] { id }));
        Assert.That(missing.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>A draft is deleted once; again, or an unknown id, is a 404.</summary>
    [Test]
    public async Task ADraft_IsDeletedOnce()
    {
        await using var host = await StartAsync();
        string id = (await JsonOf(await Send(host.Client, HttpMethod.Post, Drafts, Body())))
            .GetProperty("id").GetString()!;

        var deleted = await Send(host.Client, HttpMethod.Delete, $"{Drafts}/{id}");
        var again = await Send(host.Client, HttpMethod.Delete, $"{Drafts}/{id}");
        var read = await Send(host.Client, HttpMethod.Get, $"{Drafts}/{id}");

        Assert.That((deleted.StatusCode, again.StatusCode, read.StatusCode),
            Is.EqualTo((HttpStatusCode.NoContent, HttpStatusCode.NotFound, HttpStatusCode.NotFound)));
    }
}
