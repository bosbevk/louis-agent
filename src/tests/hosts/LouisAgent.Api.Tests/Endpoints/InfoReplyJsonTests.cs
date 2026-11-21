using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Endpoints;

/// <summary>
/// Pins the JSON of what the instance says about itself (its health, model, roles, LLM profiles, toolsets and display
/// currency), which Puls, the code-review app and other hosts read, so it stays the same however the endpoints are
/// shaped. A value that depends on this machine's setup is read back from the reply, the names and their order are
/// fixed.
/// </summary>
public class InfoReplyJsonTests
{
    /// <summary>The probe says it's up, and nothing else.</summary>
    [Test]
    public async Task TheProbe_IsStatusOk()
    {
        Assert.That(await BodyAsync("/health"), Is.EqualTo("""{"status":"ok"}"""));
    }

    /// <summary>The model names its provider and the model itself.</summary>
    [Test]
    public async Task TheModel_NamesItsProviderAndModel()
    {
        using var json = JsonDocument.Parse(await BodyAsync("/v1/model"));

        Assert.That(Names(json.RootElement), Is.EqualTo("provider,model"));
        Assert.That(json.RootElement.GetProperty("provider").ValueKind, Is.EqualTo(JsonValueKind.String));
    }

    /// <summary>The roles: the default voice, then each level's roles.</summary>
    [Test]
    public async Task TheRoles_AreTheDefaultThenEachLevel()
    {
        using var json = JsonDocument.Parse(await BodyAsync("/v1/roles"));

        Assert.That(Names(json.RootElement), Is.EqualTo("default,agent,specialists,minions"));
    }

    /// <summary>The LLM profiles: no default (the instance's own model), then the profiles' names.</summary>
    [Test]
    public async Task TheLlmProfiles_HaveNoDefault()
    {
        using var json = JsonDocument.Parse(await BodyAsync("/v1/llm-profiles"));

        Assert.That(Names(json.RootElement), Is.EqualTo("default,profiles"));
        Assert.That(json.RootElement.GetProperty("default").ValueKind, Is.EqualTo(JsonValueKind.Null));
    }

    /// <summary>The toolsets: the default list, then each toolset with whether it can load here and why not.</summary>
    [Test]
    public async Task TheToolsets_SayWhichCanLoad()
    {
        using var json = JsonDocument.Parse(await BodyAsync("/v1/toolsets"));

        Assert.That(Names(json.RootElement), Is.EqualTo("default,toolsets"));
        Assert.That(Names(json.RootElement.GetProperty("toolsets")[0]),
            Is.EqualTo("name,description,available,missing_settings"));
    }

    /// <summary>The display currency, its symbol, the base, the rate's date, and a symbol per currency.</summary>
    [Test]
    public async Task TheDisplayCurrency_KeepsItsNames()
    {
        using var json = JsonDocument.Parse(await BodyAsync("/v1/rates/display"));

        Assert.That(Names(json.RootElement),
            Is.EqualTo("display_currency,display_symbol,base,rate_as_of,symbols"));
    }

    /// <summary>Asks the API (no key) and hands back the body of its 200.</summary>
    private static async Task<string> BodyAsync(string path)
    {
        await using WebApplication app = ApiTestHost.Build();
        await app.StartAsync();

        using var response = await app.GetTestClient().GetAsync(path);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), path);
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>The object's names in the order they were written.</summary>
    private static string Names(JsonElement element) =>
        string.Join(",", element.EnumerateObject().Select(p => p.Name));
}
