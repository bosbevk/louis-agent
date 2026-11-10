using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Recorded;
using LouisAgent.Web.Components.Site.Showroom;
using LouisAgent.Web.Components.Site.Showroom.Mock;
using LouisAgent.Web.Components.Site.Showroom.Stage;
using LouisAgent.Web.Components.Site.Tiles;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Components.Showroom;

/// <summary>
/// One showroom business's demo (F128.2; since F127.1 on the framework's stage): its recorded conversation played on a
/// mock of its own website, in its sample brand, with the cues of what to watch beside it and the offer under it. Each
/// page names its business, its brand, its turns and its cues; the shape is this one.
/// </summary>
/// <remarks>
/// The page's two pieces of state are the turn the play has reached and the place its agent last showed: the recorded
/// chat says each turn as it starts and each tool call as it plays, the cues light up to the one and the website goes
/// to the other, so none of the three knows the others (Mediator).
/// </remarks>
public partial class BusinessDemo
{
    /// <summary>The demo's colour, the one it wears in the showroom's rail.</summary>
    [Parameter, EditorRequired]
    public TileAccent Accent { get; set; }

    /// <summary>The kind of business, over the heading: "A restaurant".</summary>
    [Parameter, EditorRequired]
    public string Kicker { get; set; } = "";

    /// <summary>The business's name, said to be made up.</summary>
    [Parameter, EditorRequired]
    public string Business { get; set; } = "";

    /// <summary>What the page shows, in a few words: the page's main heading.</summary>
    [Parameter, EditorRequired]
    public string Heading { get; set; } = "";

    /// <summary>The scene: who asks what.</summary>
    [Parameter, EditorRequired]
    public string Lede { get; set; } = "";

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

    /// <summary>The kind of business, as the notes' heading names it: "guesthouse".</summary>
    [Parameter, EditorRequired]
    public string KindOf { get; set; } = "";

    /// <summary>
    /// What a custom agent could add for a business like this one (Louis: "we could integrate with the booking system
    /// if they have one etc. Custom Agent").
    /// </summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<DemoNote> CustomNotes { get; set; } = [];

    /// <summary>The conversation, a turn at a time.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<RecordedTurn> Turns { get; set; } = [];

    /// <summary>What to watch for, each about a turn.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<DemoCue> Cues { get; set; } = [];

    /// <summary>
    /// The business's identity, by the name the site's catalogue knows, for a visitor to try live:
    /// <c>seemeeu-guesthouse</c>.
    /// </summary>
    [Parameter, EditorRequired]
    public string Identity { get; set; } = "";

    /// <summary>A first question for a visitor trying it live, from what its agent knows.</summary>
    [Parameter, EditorRequired]
    public string Hint { get; set; } = "";

    /// <summary>The turn the play has reached; none before it starts.</summary>
    private int _reached;

    /// <summary>What the demo is, over its title: the kind and the business.</summary>
    private string KickerLine => $"{Kicker} · {Business}";

    /// <summary>The widget's title on the business's website.</summary>
    private string AgentLabel => $"{Business} · ask us";

    /// <summary>The place on its website its agent last showed; none at home.</summary>
    private string? _shown;

    /// <summary>The recorded chat, which a clicked cue plays from its turn.</summary>
    private RecordedChat? _chat;

    /// <summary>Whether the visitor is trying the agent live rather than watching the recording.</summary>
    private bool _trying;

    [Inject] private IServiceProvider Services { get; set; } = null!;

    /// <summary>The showroom's words, the site's own when it has them.</summary>
    private ShowroomText Words => Services.GetService<ShowroomText>() ?? new ShowroomText();

    /// <summary>
    /// A clicked cue plays the conversation from its turn (Louis: "this should be clickable so visitors can rewind").
    /// </summary>
    private Task PlayFrom(int turn) =>
        _trying ? Task.CompletedTask : _chat?.PlayFromAsync(turn) ?? Task.CompletedTask;

    /// <summary>
    /// Notes the turn the play has reached, so the cues light up to it; a play from the start brings the website
    /// home.
    /// </summary>
    private void Reach(int turn)
    {
        _reached = turn;
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
        // The recorded chat goes with the swap; a new one is drawn on the way back.
        _chat = null;
    }

    /// <summary>Back to the recording, which plays again from its start.</summary>
    private void Watch() => _trying = false;
}
