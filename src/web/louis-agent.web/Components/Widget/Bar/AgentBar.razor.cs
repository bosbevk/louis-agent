using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Widget.Bar.Caption;
using LouisAgent.Web.Components.Widget.Orb;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Components.Widget.Bar;

/// <summary>
/// The chat widget's bar (F134.3): the guide at the foot of the page. Over it the talk so far, folded to its end until
/// the visitor opens it; in it the orb, a line of what the guide is doing, and the box, always there to type in.
/// </summary>
/// <remarks>
/// A view of the widget's own runner, not a second chat: it reads the runner and hands a send or a choice back to the
/// widget, so the session, the door, the page that travels with a message and the tool calls stay in one place (F134's
/// rule 4, one engine with a layout). What it shows of the agent is read from the runner by
/// <see cref="OrbStates"/>, never guessed. It hears a place being shown itself, since only the bar shows it.
/// </remarks>
public partial class AgentBar : IDisposable
{
    /// <summary>How long the orb shows a place after the guide's call to show it, since the call has no end.</summary>
    private static readonly TimeSpan ShowingFor = TimeSpan.FromSeconds(2.5);

    /// <summary>The widget's runner: the chat, the draft and whether it is streaming.</summary>
    [Parameter, EditorRequired]
    public ChatRunner Runner { get; set; } = null!;

    /// <summary>Who the guide is: the orb's name, and the line's words while it waits.</summary>
    [Parameter]
    public string Title { get; set; } = "";

    /// <summary>What the empty box says.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>The currency the talk's numbers are shown in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>Whether the talk shows the agent's work, as the widget's panel does.</summary>
    [Parameter]
    public bool ShowWork { get; set; } = true;

    /// <summary>Whether a card's answer shows in the talk, as the widget's panel does.</summary>
    [Parameter]
    public bool ShowCardAnswers { get; set; }

    /// <summary>Asks the widget to send what is in the box.</summary>
    [Parameter]
    public EventCallback OnSend { get; set; }

    /// <summary>Asks the widget to answer with a choice the guide offered.</summary>
    [Parameter]
    public EventCallback<string> OnChoose { get; set; }

    /// <summary>The chat's words.</summary>
    [Inject]
    private ChatText Text { get; set; } = null!;

    /// <summary>The site's services, where its guide's suggestions are when it names any.</summary>
    [Inject]
    private IServiceProvider Services { get; set; } = null!;

    /// <summary>Whether the guide is talking, told to the page when the site keeps it (F134.4).</summary>
    private GuideActivity? Activity => Services.GetService<GuideActivity>();

    /// <summary>The questions the site's guide offers as chips; none when the site names none.</summary>
    private IReadOnlyList<string>? Chips => Services.GetService<AgentSuggestions>()?.Questions;

    /// <summary>Whether the talk is open whole, or folded to its end.</summary>
    private bool _talkOpen;

    /// <summary>When the guide last showed a place, while the orb still shows it.</summary>
    private DateTimeOffset? _shownAt;

    /// <summary>The runner this bar listens to, so a new one is heard and an old one let go.</summary>
    private ChatRunner? _heard;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_heard, Runner)) return;
        if (_heard is not null) _heard.ToolCalled -= OnToolCalled;
        if (_heard is not null) _heard.Changed -= OnChanged;
        _heard = Runner;
        _heard.ToolCalled += OnToolCalled;
        _heard.Changed += OnChanged;
    }

    /// <summary>
    /// The runner moved on: the page is told whether the guide is answering. The runner tells from its stream and from
    /// its redraw timer, so the telling waits its turn on the bar's own dispatcher and reads the runner as it is then;
    /// a late timer's call can't leave the guide talking after the answer ended.
    /// </summary>
    private void OnChanged() => _ = InvokeAsync(() => Activity?.Set(Runner.Streaming));

    /// <summary>The guide called a tool: a call to show a place lights the orb's showing for a moment.</summary>
    private void OnToolCalled(Block block)
    {
        if (ShowPageCall.PathOf(block) is null) return;
        _shownAt = DateTimeOffset.UtcNow;
        _ = InvokeAsync(async () =>
        {
            StateHasChanged();
            await Task.Delay(ShowingFor);
            StateHasChanged();
        });
    }

    /// <summary>Whether a place was shown a moment ago.</summary>
    private bool Showing => _shownAt is { } at && DateTimeOffset.UtcNow - at < ShowingFor;

    /// <summary>What the orb shows, read from the runner.</summary>
    private OrbState State =>
        OrbStates.Of(Runner.Streaming, Runner.Chat?.Turns.LastOrDefault()?.EndsInWords == true, Showing);

    /// <summary>The line over the box: what the guide is doing, or its name while it waits.</summary>
    private string Doing => State switch
    {
        OrbState.Thinking => Text.Thinking,
        OrbState.Speaking => Text.AgentSpeaking,
        OrbState.Showing => Text.AgentShowing,
        _ => Title
    };

    /// <summary>
    /// The last answer as a caption (F134.4), once it is done and while the visitor hasn't opened the whole talk.
    /// </summary>
    private Captioned? Caption =>
        _talkOpen || Runner.Streaming ? null : Captioned.Of(Runner.Chat?.Turns.LastOrDefault());

    /// <summary>The talk's classes: open whole, or folded to its end.</summary>
    private string TalkClasses => _talkOpen ? "agent-talk is-open" : "agent-talk";

    /// <summary>The talk's button: what a press does.</summary>
    private string TalkLabel => _talkOpen ? Text.TalkClose : Text.TalkOpen;

    /// <summary>Opens the talk whole, or folds it to its end.</summary>
    private void ToggleTalk() => _talkOpen = !_talkOpen;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_heard is not null) _heard.ToolCalled -= OnToolCalled;
        if (_heard is not null) _heard.Changed -= OnChanged;
    }
}
