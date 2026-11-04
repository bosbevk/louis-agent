using System.Net;
using System.Text;
using System.Text.Json;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;

namespace LouisAgent.Web.Shared.Tests.Api;

/// <summary>
/// The portal reads a reply straight into its record with the wire's options, instead of reading a
/// <c>JsonElement</c> first and turning it into the record after.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public class AgentApiGetTests
{
    /// <summary>A reply on a snake_case wire, as the admin routes write it.</summary>
    private sealed record RunReply(string RunId, int ExitCode);

    /// <summary>The admin wire's options: snake_case names.</summary>
    private static readonly JsonSerializerOptions SnakeCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    /// <summary>A client whose server answers every call with this status and JSON.</summary>
    private static AgentApi Api(HttpStatusCode status, string json) =>
        new(
            new HttpClient(new StubHandler(_ =>
                new HttpResponseMessage(status)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                }))
            { BaseAddress = new Uri("http://localhost/") }, new ChatSettings());

    /// <summary>The reply comes back as its record, its snake_case names read with the options given.</summary>
    [Test]
    public async Task AReply_IsReadAsItsRecord_WithTheWiresOptions()
    {
        var api = Api(HttpStatusCode.OK, """{"run_id":"r1","exit_code":3}""");

        var reply = await api.GetAsync<RunReply>("v1/admin/evals/runs/r1", SnakeCase);

        Assert.That(reply, Is.EqualTo(new RunReply("r1", 3)));
    }

    /// <summary>An error is the API's exception with its words, as every other call.</summary>
    [Test]
    public void AnError_IsAnApiException()
    {
        var api = Api(HttpStatusCode.NotFound,
            """{"type":"error","error":{"type":"not_found","message":"No run."}}""");

        Assert.That(async () => await api.GetAsync<RunReply>("v1/x", SnakeCase),
            Throws.InstanceOf<AgentApiException>().With.Property(nameof(AgentApiException.Message))
                .EqualTo("No run."));
    }
}
