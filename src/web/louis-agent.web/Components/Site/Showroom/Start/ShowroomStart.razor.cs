using LouisAgent.Choices;
using LouisAgent.Web.Components.Site.Tiles;
using LouisAgent.Web.Components.Staging.Docks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Components.Site.Showroom.Start;

/// <summary>
/// The showroom's start (F134.1): a stage. Every demo of the site is a framed window on a deck, one forward in the
/// light. Under it are the forward demo's name, its line and its way in, and a dock with a button per demo.
/// </summary>
/// <remarks>
/// The start holds one thing, which demo is forward, and puts the framework's general parts together around it: the
/// lit stage, the deck, the framed window and the dock. Each part is told which demo is forward and says what a
/// visitor asked for, and none of them changes it, so the deck's arrows and the dock's buttons can't disagree. That
/// is state held by the nearest common parent, with parameters down and callbacks up (Microsoft's Blazor guidance on
/// component parameters and event callbacks). The windows are drawn from the site's
/// <see cref="ShowroomDemos"/>, so a new demo is one line in the site and stands here too.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
/// <seealso href="https://learn.microsoft.com/aspnet/core/blazor/components/event-handling#eventcallback">EventCallback (Microsoft Learn)</seealso>
public partial class ShowroomStart
{
    /// <summary>What a visitor brought forward, by its place in the site's list. None until they do.</summary>
    private int? _brought;

    /// <summary>The address of the demo that is forward first, such as <c>/guesthouse-demo</c>.</summary>
    [Parameter, EditorRequired]
    public string Featured { get; set; } = "";

    /// <summary>The start's title, the page's one heading.</summary>
    [Parameter, EditorRequired]
    public RenderFragment Title { get; set; } = null!;

    /// <summary>A line under the title.</summary>
    [Parameter]
    public RenderFragment? Lede { get; set; }

    [Inject] private IServiceProvider Services { get; set; } = null!;

    /// <summary>The site's demos; none when it registered none.</summary>
    private ShowroomDemos Demos => Services.GetService<ShowroomDemos>() ?? new ShowroomDemos([]);

    /// <summary>The site's words, else the framework's English.</summary>
    private ShowroomText Text => Services.GetService<ShowroomText>() ?? new ShowroomText();

    /// <summary>The demo that is forward; none when the site has no demos.</summary>
    private ShowroomDemo? Forward => Demos.All.Count == 0 ? null : Demos.All[ForwardAt];

    /// <summary>
    /// Where the forward demo is in the site's list: what the visitor brought forward, else the featured one.
    /// </summary>
    private int ForwardAt => _brought ?? FeaturedAt;

    /// <summary>
    /// Where the featured demo is in the site's list. The first, when the site names one it doesn't have.
    /// </summary>
    private int FeaturedAt
    {
        get
        {
            var featured = Demos.At(Featured);
            return Demos.All.Select((demo, at) => (Demo: demo, At: at)).FirstOrDefault(place => place.Demo == featured)
                .At;
        }
    }

    /// <summary>The stage's colour: the forward demo's, the showroom's purple when the site has no demos.</summary>
    private TileAccent StageAccent => Forward?.Accent ?? TileAccent.Purple;

    /// <summary>A button per demo for the dock, in the site's order, grouped as the demos are.</summary>
    private IReadOnlyList<DockItem> DockItems =>
    [
        .. Demos.All.Select(demo => new DockItem(IdOf(demo), demo.Name, demo.Icon, demo.Accent, demo.Group.Wire()))
    ];

    /// <summary>A demo's id on the page: its address without the slash, such as <c>guesthouse-demo</c>.</summary>
    private static string IdOf(ShowroomDemo demo) => demo.Address.Trim('/');

    /// <summary>The word in a demo's window bar: the one it names, else its own address.</summary>
    private static string AddressWordOf(ShowroomDemo demo) => demo.WindowAddress ?? demo.Address;

    /// <summary>A window's classes: in its demo's colour, so its ring and its plain body wear it.</summary>
    private static string WindowClasses(ShowroomDemo demo) => $"start-window accent-{demo.Accent.Wire()}";

    /// <summary>The deck asked for the demo at this place to come forward.</summary>
    private void Bring(int at) => _brought = at;

    /// <summary>The dock's button for a demo was pressed: that demo comes forward.</summary>
    private void Choose(DockItem pressed)
    {
        // The button's id is its demo's, so the place in the list is found by it.
        var places = Demos.All.Select((demo, at) => (Id: IdOf(demo), At: at));
        _brought = places.First(place => place.Id == pressed.Id).At;
    }
}
