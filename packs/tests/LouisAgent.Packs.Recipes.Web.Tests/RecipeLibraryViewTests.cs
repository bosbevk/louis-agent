using System.Net;
using System.Net.Http.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Choices;
using LouisAgent.Packs.Recipes.Library.Screens;
using LouisAgent.Packs.Recipes.Library.Usage;
using LouisAgent.Composition.Packs.Screens.Reading;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Packs;
using LouisAgent.Web.Components.Screens.Table;
using LouisAgent.Web.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Packs.Recipes.Web.Tests;

/// <summary>
/// The recipe library's own view: the plain table with one line above it. This project sees both sides, so it also
/// holds together the names the pack and its view share, since neither may reference the other.
/// </summary>
public class RecipeLibraryViewTests
{
    /// <summary>The library's real screen, as the API would describe it.</summary>
    private static readonly RecipesUsageScreen RealScreen = new(new NoRecipeUsage());

    /// <summary>The description as the browser receives it.</summary>
    private static ScreenReply Described() => new(RealScreen.Spec.Name, RealScreen.Spec.Title, RealScreen.Spec.Description,
        [.. RealScreen.Spec.Columns.Select(column => new ScreenColumnReply(column.Title, column.Kind.Wire()))],
        RealScreen.Spec.Searchable);

    /// <summary>The rows the real screen writes for one day, as the API would hand them on.</summary>
    private static async Task<ScreenPageReply> PageOf(long offered, long served)
    {
        var usage = new RecipeUsageDay(new DateOnly(2026, 11, 6), 3, offered, served, 0);
        var screen = new RecipesUsageScreen(new OneDayUsage(usage));
        var page = await screen.ReadAsync(new ScreenQuery(null, 1), CancellationToken.None);
        return new ScreenPageReply([.. page.Rows.Select(row => new ScreenRowReply(row.Id, row.Cells))], page.Total, 50);
    }

    /// <summary>A counter holding one day.</summary>
    private sealed class OneDayUsage(RecipeUsageDay day) : IRecipeUsage
    {
        /// <inheritdoc />
        public Task AddAsync(RecipeUsageKind kind, long by, DateOnly on, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        /// <inheritdoc />
        public Task<IReadOnlyList<RecipeUsageDay>> DaysAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RecipeUsageDay>>([day]);
    }

    /// <summary>The view over an API answering any rows request with this page.</summary>
    private static IRenderedComponent<RecipeLibraryView> Render(BunitContext ctx, ScreenPageReply page)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(page, options: AdminJson.SnakeCase)
            }))
        {
            BaseAddress = new Uri("http://localhost/")
        }, settings));

        var view = ctx.Render<RecipeLibraryView>(p => p.Add(c => c.Screen, Described()));
        view.WaitForState(() => view.FindAll(".screen-loading").Count == 0, TimeSpan.FromSeconds(10));
        return view;
    }

    /// <summary>The line reads from the real screen's rows, and the table stays below it.</summary>
    [Test]
    public async Task TheLine_ReadsOfOffered_Served_AndTheTableStays()
    {
        using var ctx = new BunitContext();

        var view = Render(ctx, await PageOf(offered: 10, served: 1));

        Assert.That(view.Find(".screen-above").TextContent, Is.EqualTo("Of 10 offered, 1 served"));
        Assert.That(view.FindAll("table"), Has.Count.EqualTo(1));
    }

    /// <summary>With nothing counted there's no line, only what the table says.</summary>
    [Test]
    public void WithNoRows_ThereIsNoLine()
    {
        using var ctx = new BunitContext();

        var view = Render(ctx, new ScreenPageReply([], 0, 50));

        Assert.That(view.FindAll(".screen-above"), Is.Empty);
        Assert.That(view.FindAll(".screen-empty"), Has.Count.EqualTo(1));
    }

    /// <summary>A view of a page that has no all-days line shows no made-up numbers.</summary>
    [Test]
    public void WithNoAllDaysLine_ThereIsNoLine()
    {
        using var ctx = new BunitContext();
        var page = new ScreenPageReply([new ScreenRowReply("2026-11-06", ["2026-11-06", "1", "2", "3", "4"])], 1, 50);

        var view = Render(ctx, page);

        Assert.That(view.FindAll(".screen-above"), Is.Empty);
    }

    /// <summary>The view registers under the name the pack gave its screen, and a demo can still take over.</summary>
    [Test]
    public void TheView_RegistersUnderThePacksName()
    {
        var views = new PackViews();

        RecipeWebViews.AddTo(views);

        Assert.That(views.ScreenView(RealScreen.Spec.Name), Is.EqualTo(typeof(RecipeLibraryView)));
        views.UseScreen<ScreenTable>(RealScreen.Spec.Name);
        Assert.That(views.ScreenView(RealScreen.Spec.Name), Is.EqualTo(typeof(ScreenTable)));
    }
}
