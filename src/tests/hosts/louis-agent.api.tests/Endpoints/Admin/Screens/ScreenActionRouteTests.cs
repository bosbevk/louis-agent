using System.Net;
using System.Net.Http.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Composition.Packs.Screens.Acting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Api.Tests.Endpoints.Admin.Screens;

/// <summary>
/// A screen's actions over the API: only what its pack named can be asked for, behind the admin key, and every one is
/// logged with its row, so whoever runs it can see what was done.
/// </summary>
public class ScreenActionRouteTests
{
    private const string AdminKey = "admin-secret";
    private const string Remove = "/v1/admin/screens/usage/actions/remove";

    /// <summary>Keeps every log line the app writes.</summary>
    private sealed class KeptLogs : ILoggerProvider, ILogger
    {
        public List<string> Lines { get; } = [];

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName) => this;

        /// <inheritdoc />
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel) => true;

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            lock (Lines) Lines.Add(formatter(state, exception));
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }

    /// <summary>The API with this screen, a client carrying the admin key, and what it logs.</summary>
    private static async Task<(WebApplication App, HttpClient Client, KeptLogs Logs)> StartAsync(FakeScreen screen)
    {
        var app = ApiTestHost.Build(adminKey: AdminKey, morePacks: [new FakePack("screener", toolsets: [], screens: [screen])]);
        var logs = new KeptLogs();
        app.Services.GetRequiredService<ILoggerFactory>().AddProvider(logs);
        await app.StartAsync();
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("x-api-key", AdminKey);
        return (app, client, logs);
    }

    /// <summary>A screen that can remove a row, with a question first.</summary>
    private static FakeScreen Removing() => new FakeScreen("usage").With(
        new ScreenAction("remove", "Remove", ScreenActionOn.Row, "Remove it?"),
        new ScreenAction("clear", "Clear all", ScreenActionOn.Screen));

    /// <summary>The body of a request about this row.</summary>
    private static JsonContent AboutRow(string? rowId) =>
        JsonContent.Create(new ScreenActionRequest(rowId), options: AdminJson.SnakeCase);

    /// <summary>The description says what can be done, so the page can draw the buttons.</summary>
    [Test]
    public async Task TheDescription_ListsTheActions()
    {
        var (app, client, _) = await StartAsync(Removing());
        await using var _ = app;

        var reply = await client.GetFromJsonAsync<ScreensReply>("/v1/admin/screens", AdminJson.SnakeCase);

        var remove = reply!.Screens.Single().Actions[0];
        Assert.That(remove, Is.EqualTo(new ScreenActionReply("remove", "Remove", "row", "Remove it?")));
    }

    /// <summary>The pack's answer comes back, and the row reached it.</summary>
    [Test]
    public async Task AnAction_ReachesThePack_AndItsAnswerComesBack()
    {
        var screen = Removing();
        var (app, client, _) = await StartAsync(screen);
        await using var __ = app;

        var reply = await client.PostAsync(Remove, AboutRow("row-1"));

        var answer = await reply.Content.ReadFromJsonAsync<ScreenAnswerReply>(AdminJson.SnakeCase);
        Assert.That(screen.LastAction, Is.EqualTo(("remove", "row-1")));
        Assert.That(answer!.Done, Is.True);
        Assert.That(answer.Message, Is.EqualTo("remove done"));
    }

    /// <summary>What it did is logged, with the screen, the action and the row.</summary>
    [Test]
    public async Task AnAction_IsLogged_WithItsRow()
    {
        var (app, client, logs) = await StartAsync(Removing());
        await using var __ = app;

        await client.PostAsync(Remove, AboutRow("row-1"));

        Assert.That(logs.Lines, Has.Some.Contains("usage").And.Contains("remove").And.Contains("row-1"));
    }

    /// <summary>A screen-wide action needs no row.</summary>
    [Test]
    public async Task AScreenAction_NeedsNoRow()
    {
        var screen = Removing();
        var (app, client, _) = await StartAsync(screen);
        await using var __ = app;

        var reply = await client.PostAsync("/v1/admin/screens/usage/actions/clear", AboutRow(null));

        Assert.That(reply.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(screen.LastAction, Is.EqualTo(("clear", (string?)null)));
    }

    /// <summary>A row's action without a row is refused before it reaches the pack.</summary>
    [Test]
    public async Task ARowAction_WithNoRow_IsRefused()
    {
        var screen = Removing();
        var (app, client, _) = await StartAsync(screen);
        await using var __ = app;

        var reply = await client.PostAsync(Remove, AboutRow(null));

        Assert.That(reply.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(screen.LastAction, Is.Null);
    }

    /// <summary>An action the pack never named doesn't exist, and a screen no pack offers doesn't either.</summary>
    [TestCase("/v1/admin/screens/usage/actions/format-the-disk")]
    [TestCase("/v1/admin/screens/nothing/actions/remove")]
    public async Task WhatWasNeverOffered_IsNotFound(string address)
    {
        var screen = Removing();
        var (app, client, _) = await StartAsync(screen);
        await using var __ = app;

        var reply = await client.PostAsync(address, AboutRow("row-1"));

        Assert.That(reply.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(screen.LastAction, Is.Null);
    }

    /// <summary>No key, no action.</summary>
    [Test]
    public async Task WithoutTheAdminKey_NothingHappens()
    {
        var screen = Removing();
        var (app, _, _) = await StartAsync(screen);
        await using var __ = app;

        var reply = await app.GetTestClient().PostAsync(Remove, AboutRow("row-1"));

        Assert.That(reply.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(screen.LastAction, Is.Null);
    }
}
