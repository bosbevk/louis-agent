using System.Text.Json;
using LouisAgent.Api.Endpoints.Answers;
using Microsoft.AspNetCore.Http;

namespace LouisAgent.Api.Tests.Endpoints;

/// <summary>
/// A 201 on a wire with its own JSON options: it names what it made in Location and writes the value with that wire's
/// options, which the framework's own <c>Created</c> can't (the unattended run's finding 2).
/// </summary>
public class CreatedJsonTests
{
    private static readonly JsonSerializerOptions Snake = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    /// <summary>The answer is a 201 with its Location, and the body uses the options it was given.</summary>
    [Test]
    public async Task ItWrites201_TheLocation_AndTheValueWithTheWiresOptions()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await new CreatedJson<Sample>("/v1/things/1", new Sample(3), Snake).ExecuteAsync(context);

        context.Response.Body.Position = 0;
        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status201Created));
        Assert.That(context.Response.Headers.Location.ToString(), Is.EqualTo("/v1/things/1"));
        Assert.That(await new StreamReader(context.Response.Body).ReadToEndAsync(), Is.EqualTo("""{"exit_code":3}"""));
    }

    /// <summary>A test can read the status and the value with no HTTP at all.</summary>
    [Test]
    public void ItSaysItsStatusAndValue()
    {
        var created = new CreatedJson<Sample>("/v1/things/1", new Sample(3), Snake);

        Assert.That(created.StatusCode, Is.EqualTo(StatusCodes.Status201Created));
        Assert.That(created.Value, Is.EqualTo(new Sample(3)));
    }

    /// <summary>A value with a name of two words, so the options show in what's written.</summary>
    private sealed record Sample(int ExitCode);
}
