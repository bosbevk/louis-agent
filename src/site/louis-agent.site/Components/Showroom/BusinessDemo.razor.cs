using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Recorded;
using LouisAgent.Web.Components.Site.Showroom;
using LouisAgent.Web.Components.Site.Showroom.Mock;
using LouisAgent.Web.Components.Site.Tiles;
using LouisAgent.Web.Components.Widget.Bar;
using LouisAgent.Web.Components.Widget.Door;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Site.Components.Showroom;

/// <summary>
/// One showroom business's demo (F128.2; since F127.1 on the framework's stage): its recorded conversation played on a
/// mock of its own website, in its sample brand, and nothing beside it, so the agent is the show (FX194, Louis: "the
/// rest takes the shine away from the real AI"). Each page names its business, its brand and its turns; the shape is
/// this one.
/// </summary>
/// <remarks>
/// The page's state is the place its agent last showed: the recorded chat says each tool call as it plays and the
/// website goes there, so neither knows the other (Mediator).
/// </remarks>
public partial class BusinessDemo : IDisposable
{
    /// <summary>The demo's colour, the one it wears in the showroom's rail.</summary>
    [Parameter, EditorRequired]
    public TileAccent Accent { get; set; }

    /// <summary>The kind of business, over the heading: "A restaurant".</summary>
    [Parameter, EditorRequired]
    public string Topic { get; set; } = "";

    /// <summary>The business's name, said to be made up.</summary>
    [Parameter, EditorRequired]
    public string Business { get; set; } = "";

    /// <summary>What the page shows, in a few words: the page's main heading.</summary>
    [Parameter, EditorRequired]
    public string Heading { get; set; } = "";

    /// <summary>The scene: who asks what.</summary>
    [Parameter, EditorRequired]
    public string Summary { get; set; } = "";

    /// <summary>The business's own address, made up: <c>wheatfieldkitchen.example</c>.</summary>
    [Parameter, EditorRequired]
    public string Address { get; set; } = "";

    /// <summary>Its sample brand's colours.</summary>
    [Parameter, EditorRequired]
    public MockColours Colours { get; set; } = null!;

    /// <summary>Its sample brand's typeface.</summary>
    [Parameter]
    public MockFont Font { get; set; }

    /// <summary>Its sample brand's mark, drawn for it.</summary>
    [Parameter, EditorRequired]
    public RenderFragment Mark { get; set; } = null!;

    /// <summary>The few words under its name.</summary>
    [Parameter, EditorRequired]
    public string Tagline { get; set; } = "";

    /// <summary>Its website's hero line, from what its agent knows.</summary>
    [Parameter, EditorRequired]
    public string HeroLine { get; set; } = "";

    /// <summary>A few words under the hero line, from what its agent knows.</summary>
    [Parameter]
    public string? HeroNote { get; set; }

    /// <summary>Its website's header, as its brand wears it (F127.2).</summary>
    [Parameter]
    public MockHeader Header { get; set; }

    /// <summary>Its website's look (Louis: "give each one a different look").</summary>
    [Parameter]
    public MockLook Look { get; set; }

    /// <summary>Its website's hero photo, from the site's own images, worn with its brand.</summary>
    [Parameter, EditorRequired]
    public string HeroPhoto { get; set; } = "";

    /// <summary>What the hero's photo shows, for a screen reader.</summary>
    [Parameter, EditorRequired]
    public string HeroPhotoAlt { get; set; } = "";

    /// <summary>Its website's call to action: "Book a table".</summary>
    [Parameter, EditorRequired]
    public string HeroAction { get; set; } = "";

    /// <summary>A few facts under its website's hero, from what its agent knows.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<string> Facts { get; set; } = [];

    /// <summary>
    /// Its website's pages (F127.2), each with its places at their addresses, the pages and places its agent can show.
    /// </summary>
    [Parameter, EditorRequired]
    public RenderFragment Pages { get; set; } = null!;

    /// <summary>The conversation, a turn at a time.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<RecordedTurn> Turns { get; set; } = [];

    /// <summary>
    /// The business's identity, by the name the site's catalogue knows, for a visitor to try live:
    /// <c>seemeeu-guesthouse</c>.
    /// </summary>
    [Parameter, EditorRequired]
    public string Identity { get; set; } = "";

    /// <summary>A first question for a visitor trying it live, from what its agent knows.</summary>
    [Parameter, EditorRequired]
    public string Hint { get; set; } = "";

    /// <summary>What the demo is, over its title: the kind and the business.</summary>
    private string KickerLine => $"{Topic} · {Business}";

    /// <summary>The widget's title on the business's website.</summary>
    private string AgentLabel => $"{Business} · ask us";

    /// <summary>The place on its website its agent last showed; none at home.</summary>
    private string? _shown;

    /// <summary>Whether the visitor is trying the agent live rather than watching the recording.</summary>
    private bool _trying;

    [Inject] private IServiceProvider Services { get; set; } = null!;

    /// <summary>The showroom's words, the site's own when it has them.</summary>
    private ShowroomText Words => Services.GetService<ShowroomText>() ?? new ShowroomText();

    /// <summary>A play from the start brings the website home.</summary>
    private void Reach(int turn)
    {
        if (turn == 1) _shown = null;
    }

    /// <summary>A ShowPage call played from the recording takes the website there; any other call, nowhere.</summary>
    private void Show(Block call) => _shown = ShowPageCall.PathOf(call) ?? _shown;

    /// <summary>
    /// A visitor clicked through the website (F127.2): it stays where they went, until its agent shows the next place.
    /// </summary>
    private void Go(string path) => _shown = path;

    /// <summary>
    /// The visitor tries the agent themselves (F127.2): the live chat takes the recording's place, and the website goes
    /// home, where a new customer would start.
    /// </summary>
    private void Try()
    {
        _trying = true;
        _shown = null;
    }

    /// <summary>Back to the recording, which plays again from its start.</summary>
    private void Watch()
    {
        _trying = false;
        _asked = null;
    }

    /// <summary>
    /// The guide's ask to this business, which the live window shows (F127.4.2); none until the guide asks.
    /// </summary>
    private Block? _asked;

    /// <summary>This demo's hold on its business in the page's windows, let go with the demo.</summary>
    private IDisposable? _window;

    /// <summary>The mocked website the business sits in, whose page goes with the guide's ask to it (FX134).</summary>
    private SiteMock? _mock;

    /// <summary>The page's windows, if the site has them.</summary>
    private AgentWindows? Windows => Services.GetService<AgentWindows>();

    /// <summary>Whether the site's guide is talking, if the site keeps it (F134.4).</summary>
    private GuideActivity? Activity => Services.GetService<GuideActivity>();

    /// <summary>The recording holds while the site's guide talks, so the visitor follows one voice at a time.</summary>
    private bool GuideTalking => Activity?.Talking == true;

    /// <summary>
    /// The demo hears the guide start and stop talking, and shows its business, so an ask the guide makes to it comes
    /// here.
    /// </summary>
    protected override void OnInitialized()
    {
        if (Activity is { } activity) activity.Changed += OnGuide;
        if (Windows is not { } windows) return;
        // The window's page is its website's, read as its own message reads it, so the guide's ask can send it on.
        _window = windows.Show(Identity, () => _mock?.ReadPageAsync() ?? Task.FromResult<Page?>(null));
        windows.Asked += OnAsked;
        windows.Turned += OnTurned;
    }

    /// <summary>The conversation's place on the page, which the guide shows a turn at a time (F160).</summary>
    private const string ConversationPlace = "the-conversation";

    /// <summary>The recorded conversation, which plays the turn the guide shows.</summary>
    private RecordedChat? _recorded;

    /// <summary>
    /// The guide showed a place on the page (F160): the conversation plays that turn and stops after it. While the
    /// visitor talks to the business live, the talk is theirs, and is left as it is.
    /// </summary>
    private void OnTurned(string place, int turn)
    {
        if (place != ConversationPlace || _trying || _recorded is not { } recorded) return;
        _ = InvokeAsync(() => recorded.PlayTurnAsync(turn));
    }

    /// <summary>
    /// The guide asked this business for the visitor (F127.4.2): the live chat takes the recording's place and shows
    /// that talk, from the website's home.
    /// </summary>
    private void OnAsked(Block call) => InvokeAsync(() =>
    {
        if (Delegation.From(call)?.Agent is not { } agent ||
            !string.Equals(agent, Identity, StringComparison.OrdinalIgnoreCase)) return;
        if (!_trying) Try();
        _asked = call;
        StateHasChanged();
    });

    /// <summary>The guide started or stopped talking: the recording is told whether to hold.</summary>
    private void OnGuide() => InvokeAsync(StateHasChanged);

    /// <summary>Lets go of the business, the windows' news and the guide's with the demo.</summary>
    public void Dispose()
    {
        if (Activity is { } activity) activity.Changed -= OnGuide;
        if (Windows is { } windows)
        {
            windows.Asked -= OnAsked;
            windows.Turned -= OnTurned;
        }

        _window?.Dispose();
        GC.SuppressFinalize(this);
    }
}
