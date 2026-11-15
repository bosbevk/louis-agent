using LouisAgent.Choices;
using LouisAgent.Web.Components.Site.Addresses;
using LouisAgent.Web.Components.Site.Tiles;
using LouisAgent.Web.Components.Staging.Docks;
using LouisAgent.Web.Scripts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Components.Site.Showroom.Start;

/// <summary>
/// The showroom's start (F134.1): a stage. Every demo it shows is a framed window on a deck, one forward in the
/// light. Under it are the forward demo's name, its line and its way in, and a dock with a button per demo.
/// </summary>
/// <remarks>
/// <para>
/// The start holds one thing, which demo is forward, and puts the framework's general parts together around it: the
/// lit stage, the deck, the framed window and the dock. Each part is told which demo is forward and says what a
/// visitor asked for, and none of them changes it, so the deck's arrows and the dock's buttons can't disagree. That
/// is state held by the nearest common parent, with parameters down and callbacks up (Microsoft's Blazor guidance on
/// component parameters and event callbacks). The windows are drawn from the site's
/// <see cref="ShowroomDemos"/>, so a new demo is one line in the site and stands here too.
/// </para>
/// <para>
/// The page may name one group of the site's demos, and then the start shows that group alone. Which demos stand
/// on the start is the page's choice, handed down as a parameter like which one is forward. Chosen over a mark on
/// each demo's record, which would put the page's choice in the site's list, and over taking a demo out of that
/// list, which would take it off the rail beside every demo's page as well.
/// </para>
/// <para>
/// Each demo's window has its own address (ADR 0107). The start reads the address it was opened at, so that demo is
/// forward from the first draw in a browser. When a visitor brings another forward, a small module of the start's
/// own puts that demo's address in the bar. Chosen over the site frame's script, which follows only a card's click,
/// scrolls to the card and opens every address with the featured demo first, and over navigating through the
/// router, which draws the start afresh on the first press.
/// </para>
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0107-every-section-has-an-address.md">ADR 0107: every place has its own address</seealso>
/// <seealso href="https://learn.microsoft.com/aspnet/core/blazor/components/event-handling#eventcallback">EventCallback (Microsoft Learn)</seealso>
public partial class ShowroomStart : IAsyncDisposable
{
    /// <summary>What a visitor brought forward, by its place among the demos shown. None until they do.</summary>
    private int? _brought;

    /// <summary>The demos the start shows, in the site's order.</summary>
    private IReadOnlyList<ShowroomDemo> _shown = [];

    private JsModule? _bar;

    /// <summary>The address of the demo that is forward first, such as <c>/guesthouse-demo</c>.</summary>
    [Parameter, EditorRequired]
    public string Featured { get; set; } = "";

    /// <summary>The start's title, the page's one heading.</summary>
    [Parameter, EditorRequired]
    public RenderFragment Title { get; set; } = null!;

    /// <summary>A line under the title.</summary>
    [Parameter]
    public RenderFragment? Lede { get; set; }

    /// <summary>
    /// Which group of the site's demos the start shows, such as the ones for a visitor's own business. With none,
    /// it shows every demo. The site's list stays whole either way, for the rail beside a demo's page.
    /// </summary>
    [Parameter]
    public DemoGroup? Group { get; set; }

    [Inject] private IServiceProvider Services { get; set; } = null!;

    [Inject] private NavigationManager Nav { get; set; } = null!;

    [Inject] private IJSRuntime JS { get; set; } = null!;

    /// <summary>The site's demos; none when it registered none.</summary>
    private ShowroomDemos Demos => Services.GetService<ShowroomDemos>() ?? new ShowroomDemos([]);

    /// <summary>The site's words, else the framework's English.</summary>
    private ShowroomText Text => Services.GetService<ShowroomText>() ?? new ShowroomText();

    /// <summary>The site's addresses; none on a site whose places have none.</summary>
    private SiteAddresses? Addresses => Services.GetService<SiteAddresses>();

    /// <summary>The start's own module: the address bar following the demo that is forward.</summary>
    private JsModule Bar => _bar ??= new JsModule(JS, FrameworkScripts.ShowroomStart);

    /// <summary>The start shows the group its page named, else every demo the site has.</summary>
    protected override void OnParametersSet() =>
        _shown = [.. Demos.All.Where(demo => Group is null || demo.Group == Group)];

    /// <summary>The demo that is forward; none when the start has no demos to show.</summary>
    private ShowroomDemo? Forward => _shown.Count == 0 ? null : _shown[ForwardAt];

    /// <summary>
    /// Where the forward demo is among the demos shown: what the visitor brought forward, else the one whose
    /// address the page was opened at, else the featured one.
    /// </summary>
    private int ForwardAt => _brought ?? OpenedAt ?? FeaturedAt;

    /// <summary>
    /// Where the demo is whose address the page was opened at. None at the page's own address, at a place that is
    /// no demo shown here, and on a site without addresses.
    /// </summary>
    private int? OpenedAt => Addresses?.Read(Nav.Uri)?.Section is { } place ? PlaceOf(place) : null;

    /// <summary>
    /// Where the featured demo is among the demos shown. The first, when the page names one that isn't shown.
    /// </summary>
    private int FeaturedAt
    {
        get
        {
            var featured = Demos.At(Featured);
            return _shown.Select((demo, at) => (Demo: demo, At: at)).FirstOrDefault(place => place.Demo == featured)
                .At;
        }
    }

    /// <summary>The stage's colour: the forward demo's, the showroom's purple when the site has no demos.</summary>
    private TileAccent StageAccent => Forward?.Accent ?? TileAccent.Purple;

    /// <summary>
    /// A button per demo shown for the dock, in the site's order, grouped as the demos are. Each says which window it
    /// brings forward, so a site's guide lights the window and not the button.
    /// </summary>
    private IReadOnlyList<DockItem> DockItems => [.. _shown.Select(ButtonOf)];

    /// <summary>A demo's button on the dock.</summary>
    private static DockItem ButtonOf(ShowroomDemo demo) =>
        new(IdOf(demo), demo.Name, demo.Icon, demo.Accent, demo.Group.Wire(), WindowIdOf(demo));

    /// <summary>A demo's id on the page: its address without the slash, such as <c>guesthouse-demo</c>.</summary>
    private static string IdOf(ShowroomDemo demo) => demo.Address.Trim('/');

    /// <summary>The id of a demo's window on the deck, such as <c>guesthouse-demo-window</c>.</summary>
    private static string WindowIdOf(ShowroomDemo demo) => $"{IdOf(demo)}-window";

    /// <summary>Where the demo with this id is among the demos shown; none when none of them has it.</summary>
    private int? PlaceOf(string id) =>
        _shown.Select((demo, at) => (Id: IdOf(demo), At: (int?)at)).FirstOrDefault(place => place.Id == id).At;

    /// <summary>The word in a demo's window bar: the one it names, else its own address.</summary>
    private static string AddressWordOf(ShowroomDemo demo) => demo.WindowAddress ?? demo.Address;

    /// <summary>A window's classes: in its demo's colour, so its ring and its plain body wear it.</summary>
    private static string WindowClasses(ShowroomDemo demo) => $"start-window accent-{demo.Accent.Wire()}";

    /// <summary>
    /// The deck asked for the demo at this place to come forward. It does, and the address bar follows it.
    /// </summary>
    private async Task Bring(int at)
    {
        _brought = at;
        if (Addresses is not { } addresses) return;

        // The page's own address, whichever place on it was opened, then the forward demo's place on that page.
        string page = addresses.Read(Nav.Uri)?.Page ?? Nav.ToAbsoluteUri(Nav.Uri).AbsolutePath;
        string address = addresses.Address(page, IdOf(_shown[at]));
        await Bar.InvokeVoidAsync("show", address);
    }

    /// <summary>
    /// The dock's button for a demo was pressed: that demo comes forward, as an arrow would bring it.
    /// </summary>
    private Task Choose(DockItem pressed) => PlaceOf(pressed.Id) is { } at ? Bring(at) : Task.CompletedTask;

    /// <summary>Lets the module go, if a visitor ever brought a demo forward.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_bar is not null) await _bar.DisposeAsync();
    }
}
