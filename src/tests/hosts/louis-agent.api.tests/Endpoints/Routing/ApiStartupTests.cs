using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Endpoints.Routing;

/// <summary>
/// A program calls the API's building blocks itself, so it could forget one. Forgetting the key check must stop it
/// starting, because the agent behind these routes can run shell commands and write files.
/// </summary>
public class ApiStartupTests
{
    /// <summary>The routes won't map until the key check has run, so a program can't start open by accident.</summary>
    [Test]
    public async Task TheRoutes_RefuseToMap_WithoutTheKeyCheck()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();

        Assert.That(() => app.MapLouisAgentApi(ApiTestHost.BareHost()),
            Throws.InvalidOperationException.With.Message.Contains("UseLouisAgentKey"));
    }

    /// <summary>
    /// A host's own routes go on the API's group (F142), which is held to the same rule: no key check, no group.
    /// </summary>
    [Test]
    public async Task AHostsOwnGroup_RefusesToMap_WithoutTheKeyCheck()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();

        Assert.That(() => app.MapLouisAgentHostGroup(),
            Throws.InvalidOperationException.With.Message.Contains("UseLouisAgentKey"));
    }
}
